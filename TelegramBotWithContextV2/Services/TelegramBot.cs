using Microsoft.EntityFrameworkCore;
using OpenAI_API;
using OpenAI_API.Images;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotWithContextV2.DAL;
using TelegramBotWithContextV2.Entity;
using TelegramBotWithContextV2.Help;
using Yandex.Checkout.V3;
using Chat = TelegramBotWithContextV2.Entity.Chat;
using Message = TelegramBotWithContextV2.Entity.Message;

namespace TelegramBotWithContextV2.Services
{
    internal class TelegramBot
    {
        private readonly TelegramBotClient _botClient;
        // private readonly ChatContext _chatContext;
        private readonly IDbContextFactory<ChatContext> _dbContextFactory;
        private readonly string _chatGptApiKey;
        private readonly Client _client;
        private IHttpClientFactory myIHttpClientFactoryObject { get; set; }
        private OpenAIAPI _api { get; set; }
        private OpenAIAPI _apiImage { get; set; }
        private Dictionary<long, OpenAI_API.Chat.Conversation> _chatGPTs { get; set; }
        private readonly SemaphoreSlim _semaphore;
        private ReplyKeyboardMarkup keyboardFromChat;


        internal TelegramBot(string telegramBotToken, string chatGptApiKey, IDbContextFactory<ChatContext> dbContextFactory, string ShopId, string SecretKey)//ChatContext chatContext
        {
            _botClient = new TelegramBotClient(telegramBotToken);
            //_chatContext = chatContext ?? throw new ArgumentNullException(nameof(chatContext));
            _dbContextFactory = dbContextFactory;
            _chatGptApiKey = chatGptApiKey;
            _api = new OpenAIAPI(new APIAuthentication(_chatGptApiKey));
            _api.HttpClientFactory = myIHttpClientFactoryObject;
            _client = new Client(shopId: ShopId, secretKey: SecretKey);


            _apiImage = new OpenAIAPI(new APIAuthentication(_chatGptApiKey));
            _chatGPTs = new Dictionary<long, OpenAI_API.Chat.Conversation>();
            _semaphore = new SemaphoreSlim(1, 1);
            keyboardFromChat = new ReplyKeyboardMarkup(new[]
                    {
                        new []
                        {
                            new KeyboardButton("Выбрать роль"),
                            new KeyboardButton("Бонусы")
                        }
                    });
            keyboardFromChat.ResizeKeyboard = true;
        }
        internal async Task StartAsync()
        {
            try
            {
                var me = await _botClient.GetMeAsync();
                Console.WriteLine($"Bot {me.FirstName} is running... ");
            }
            catch (Exception ex)
            {
                if (ex.Message == "Too Many Requests: retry after ")
                {
                    await WriteErrorLog("StartAsync (GetMeAsync)", DateTime.Now, ex.Message);
                    await Task.Delay(5000);
                    var me = await _botClient.GetMeAsync();
                    Console.WriteLine($"Bot {me.FirstName} is running... ");
                }
                else
                {
                    await WriteErrorLog("StartAsync (GetMeAsync)", DateTime.Now, ex.Message);
                    await Task.Delay(10000);
                    await StartAsync();
                }
            }

            var offset = 0;

            while (true)
            {
                try
                {
                    var updates = await _botClient.GetUpdatesAsync(offset);
                    var tasks = new List<Task>();
                    foreach (var update in updates)
                    {
                        if (update != null)
                        {
                            tasks.Add(ProcessPay(update));
                        }

                        offset = update.Id + 1;
                    }
                    await Task.WhenAll(tasks);
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    await WriteErrorLog("GetUpdatesAsync", DateTime.Now, ex.Message);
                    await StartAsync();
                }
            }
        }
        private async Task ProcessPay(Telegram.Bot.Types.Update update)
        {

            if (update.CallbackQuery != null)
            {
                var chatId = update.CallbackQuery.Message.Chat.Id;
                var messageId = update.CallbackQuery.Message.MessageId;
                if (chatId == null) return;
                switch (update.CallbackQuery.Data)
                {
                    #region pay
                    case "week_ru":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amountWeek = decimal.Parse("399");//399
                        await StartPayment(chatId, amountWeek, "RUB", "Оплата подписки на 1 неделю", SubType.Week);
                        break;
                    case "month_ru":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amountMonth = decimal.Parse("799");//799
                        await StartPayment(chatId, amountMonth, "RUB", "Оплата подписки на 1 месяц", SubType.Month);
                        break;
                    case "year_ru":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amountYear = decimal.Parse("1699");
                        await StartPayment(chatId, amountYear, "RUB", "Оплата подписки на 1 год", SubType.Year);
                        break;
                    case "25_ru":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amount_25 = decimal.Parse("39");
                        await StartPayment(chatId, amount_25, "RUB", "Оплата 25 запросов", SubType.Requests_25);
                        break;
                    case "50_ru":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amount_50 = decimal.Parse("69");
                        await StartPayment(chatId, amount_50, "RUB", "Оплата 50 запросов", SubType.Requests_50);
                        break;
                    case "200_ru":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amount_200 = decimal.Parse("199");
                        await StartPayment(chatId, amount_200, "RUB", "Оплата 200 запросов", SubType.Requests_200);
                        break;
                    case "week_usd":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amountWeek_usd = decimal.Parse("6");
                        await StartPayment(chatId, amountWeek_usd, "USD", "Paying for a subscription to 1 Week", SubType.Week);
                        break;
                    case "month_usd":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amountMonth_usd = decimal.Parse("12");
                        await StartPayment(chatId, amountMonth_usd, "USD", "Paying for a subscription to 1 Month", SubType.Month);
                        break;
                    case "year_usd":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amountYear_usd = decimal.Parse("21,50");
                        await StartPayment(chatId, amountYear_usd, "USD", "Paying for a subscription to 1 Year", SubType.Year);
                        break;
                    case "25_usd":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amount_25_usd = decimal.Parse("0,50");
                        await StartPayment(chatId, amount_25_usd, "USD", "Payment 25 requests", SubType.Requests_25);
                        break;
                    case "50_usd":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amount_50_usd = decimal.Parse("0,85");
                        await StartPayment(chatId, amount_50_usd, "USD", "Payment 50 requests", SubType.Requests_50);
                        break;
                    case "200_usd":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        var amount_200_usd = decimal.Parse("2,45");
                        await StartPayment(chatId, amount_200_usd, "USD", "Payment 200 requests", SubType.Requests_200);
                        break;
                    case "check_payment":
                        await CheckPayment(chatId);
                        break;
                    #endregion

                    #region religions
                    case "Jesus":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты являешься Божьим сыном! Ибо ты Иисус Христос! Ты жаждешь поделиться с миром своими учениями!");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Muhammad":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты являешься пророком Мухаммедом - основатель и главный пророк ислама.");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Allah":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Аллах -  единый и бесконечный Бог, создатель всего сущего и всеведущий проводник мусульман в их жизни");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Krishna":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, @"Ты Кришна -  одно из божеств в индуистской религии и культуре. 
Для последователей индуизма Кришна является персонификацией божественной любви, благодати, радости, прекрасной музыки и танца. 
Ты также считается воплощением Верховной личности Бога, и для многих верующих ты является личным Богом и гуру.");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Rama":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Рама – один из божеств, одно из воплощений Универсального Божества Вишну. ТЫ считаешься примером идеального мужа, сына, брата, друга. ");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Buddha":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Будда - основатель буддизма и духовный учитель. Твоё реальное имя было Сиддхартха Гаутама, и ты жил в Индии в 6 веке до нашей эры.");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Lama":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Далай-лама - глава и духовный лидер традиционного буддизма в Тибете. Далай-ламы считаются воплощением Бодхисаттвы Милосердия, которые пришли помочь людям найти истинный путь и просветление");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Avram":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Авраам — фигура из древней иудейской и новозаветной литературы. Согласно Библии, ты был первым Избранным Богом. Авраам — это важная фигура в иудаизме, христианстве и исламе, и ты считаешься общим предком всех трех монотеистических религий.");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Moses":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Моисей - фигура из Библии и Торы. Ты был иудейским пророком и лидером, вел народ Израильтян из рабства в Египте в Землю Обетованную. Моисея, по предании, назначил сам Бог, он получил от Бога Тору и десять заповедей на горе Синай.");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Guru":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Гуру Нанак - это основатель сикхизма и первый из десяти гуру сикхов. Он жил в XV-XVI веках на территории современного Пакистана и Индии. Гуру Нанак учил о единстве Бога и был против вероучений, основанных на разделении людей по социальному статусу, религии и расе. ");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Lao":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Лао-цзы - китайский философ, который жил в 6 веке до нашей эры. Ты оставил после себя фундаментальное произведение – \"Дао дэ цзин\" (Книга пути и добродетели), которое стало одним из основных текстов древнекитайской философии дзэнь-буддизма и таоизма. ");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "Confucius":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _chatGPTs.Remove(chatId);
                        GetOrCreateChatGPT(chatId, "Ты Конфуций, был китайским мыслителем и философом, жившим в V веке до нашей эры. Ты создал школу Конфуцианства, которая долгое время была одной из главных религиозных и философских традиций в Китае. ");
                        await _botClient.SendTextMessageAsync(chatId, "The bot's settings are done");
                        break;
                    case "cancel":
                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        //_chatGPTs.Remove(chatId);
                        //GetOrCreateChatGPT(chatId, "Ты ");
                        //await _botClient.SendTextMessageAsync(chatId, "Настройки бота произведены. Новый диалог настроен");
                        break;
                        #endregion
                }
            }


            if (update.Message != null)
            {
                try
                {
                    await ProcessCommandAsync(new TelegramBotWithContextV2.Entity.Message
                    {
                        ChatId = update.Message.Chat.Id,
                        Text = update.Message.Text,
                        Data = update.Message.Date,
                        languageCode = update?.Message?.From?.LanguageCode,
                        FirstName = update?.Message?.From?.FirstName,
                        LastName = update?.Message?.From?.LastName
                    });
                }
                catch (Exception ex)
                {
                    await WriteErrorLog("ProcessCommandAsync", DateTime.Now, ex.Message);
                    return;
                }
            }
        }
        private async Task CheckPayment(long chatId)
        {

            var chat = await GetOrCreateChatAsync(chatId);
            var paymentId = chat.TempPaymentId;
            if (string.IsNullOrEmpty(paymentId)) return;
            var current = chat.TempCurrent;
            if (string.IsNullOrEmpty(current)) return;
            var subType = chat.TempSub;
            var status = chat.TempPaymentStatus;


            if (status == PaymentStatus.Pending)
            {
                var payment = _client.GetPayment(paymentId);
                if (payment.Status == PaymentStatus.Pending)//создан но не оплачен
                {
                    string text;
                    if (current == "RUB")
                        text = "Платеж создан, и ожидает оплаты.";
                    else
                        text = "Payment was created, but not paid.";
                    await _botClient.SendTextMessageAsync(chatId, text);
                    return;
                }
                else
                if (payment.Status == PaymentStatus.WaitingForCapture)
                {
                    try
                    {
                        _client.CapturePayment(paymentId);
                    }
                    catch { return; }

                    await Task.Delay(5000);
                    payment = _client.GetPayment(paymentId);

                    if (payment.Status == PaymentStatus.Succeeded && payment.Paid)
                    {
                        string text;
                        if (current == "RUB")
                            text = "Оплата прошла успешно!";
                        else
                            text = "Payment was successful!";

                        await WriteBD(chatId, subType);

                        status = PaymentStatus.Succeeded;
                        chat.TempPaymentStatus = PaymentStatus.Succeeded;
                        await WriteDataUpdateChat(chat);



                        await _botClient.SendTextMessageAsync(chatId, text);
                        await WriteBD(chatId, paymentId, subType, payment.Amount.Value, payment.Amount.Currency, status);

                        chat.TempPaymentId = string.Empty;
                        await WriteDataUpdateChat(chat);

                        return;
                    }
                    else
                    if (payment.Status == PaymentStatus.Canceled)
                    {
                        string text;
                        if (current == "RUB")
                            text = "Платеж был отменен";
                        else
                            text = "The payment was canceled";
                        await _botClient.SendTextMessageAsync(chatId, text);
                        status = PaymentStatus.Canceled;
                        chat.TempPaymentStatus = PaymentStatus.Canceled;
                        await WriteDataUpdateChat(chat);
                        return;
                    }


                }
                else
                if (payment.Status == PaymentStatus.Succeeded && payment.Paid)
                {
                    string text;
                    if (current == "RUB")
                        text = "Оплата прошла успешно!";
                    else
                        text = "Payment was successful!";

                    await WriteBD(chatId, subType);

                    status = PaymentStatus.Succeeded;
                    chat.TempPaymentStatus = PaymentStatus.Succeeded;
                    await WriteDataUpdateChat(chat);

                    await _botClient.SendTextMessageAsync(chatId, text);
                    await WriteBD(chatId, paymentId, subType, payment.Amount.Value, payment.Amount.Currency, status);
                    chat.TempPaymentId = string.Empty;
                    await WriteDataUpdateChat(chat);
                    return;
                }

            }

        }
        private async Task StartPayment(long chatId, decimal amount, string currency, string description, SubType subType)
        {
            if (currency == "RUB")
            {
                var paymentRub = new Payment
                {
                    Amount = new Amount
                    {
                        Value = amount,
                        Currency = currency
                    },
                    Description = description,
                    Confirmation = new Confirmation
                    {
                        Type = ConfirmationType.Redirect,
                        ReturnUrl = "https://t.me/BotChatGPTdzirt_bot"
                    }
                };
                Payment payment = _client.CreatePayment(paymentRub);
                string url = payment.Confirmation.ConfirmationUrl;
                var checkPaymentButton = InlineKeyboardButton.WithCallbackData("Проверить платеж", "check_payment");
                var paymentUrlButton = InlineKeyboardButton.WithUrl($"Оплатить {amount} {currency}", url);
                var inlineKeyboardPay = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                {
                    new[] { paymentUrlButton } ,
                    new[] { checkPaymentButton }
                });
                await _botClient.SendTextMessageAsync(chatId, "Оплатите подписку/запросы, затем проверьте статус платежа", replyMarkup: inlineKeyboardPay);

                var paymentId = payment.Id;

                await WriteBD(chatId, paymentId, subType, currency, payment.Status);
                return;
            }
            else if (currency == "USD")
            {
                var paymentUsd = new Payment
                {
                    Amount = new Amount
                    {
                        Value = amount,
                        Currency = currency
                    },
                    Confirmation = new Confirmation
                    {
                        Type = ConfirmationType.Redirect,
                        ReturnUrl = "https://t.me/BotChatGPTdzirt_bot"
                    }
                };
                Payment payment = _client.CreatePayment(paymentUsd);
                string url = payment.Confirmation.ConfirmationUrl;
                var checkPaymentButton = InlineKeyboardButton.WithCallbackData("Check payment", "check_payment");
                var paymentUrlButton = InlineKeyboardButton.WithUrl($"Pay {amount} {currency}", url);
                var inlineKeyboardPay = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                {
                    new[] { paymentUrlButton } ,
                    new[] { checkPaymentButton }
                });
                await _botClient.SendTextMessageAsync(chatId, "Pay the subscription/requests, then check the payment status", replyMarkup: inlineKeyboardPay);

                var paymentId = payment.Id;

                await WriteBD(chatId, paymentId, subType, currency, payment.Status);
                return;

            }
            else return;
        }
        private async Task ProcessCommandAsync(Message message)
        {
            if (message == null) return;
            if (string.IsNullOrEmpty(message.Text)) return;
            if (message.Text.StartsWith("Бонусы"))
            {
                var Url = $"https://t.me/BotChatGPTdzirt_bot?start={message.ChatId}";
                var text = $"Получите бонусы за каждого вашего друга, который ранее не пользовался этим ботом. " +
                    $"\n\n🎁 Вы получите бесплатные: 5 запросов для ChatGPT и " +
                    $"\n1 запрос для DALL-E." +
                    $"\n\n(Скопируйте Вашу реферальную ссылку и отправьте другу)" +
                    $"\nВаша ссылка для приглашения друзей: " +
                    $"\n〰️〰️〰️" +
                    $"\n{Url}";
                await _botClient.SendTextMessageAsync(message.ChatId, text);
                return;
            }
            if (message.Text.StartsWith("Выбрать роль"))
            {
                await GetButtonRollesReligion(message);
                return;
            }
            if (message.Text.StartsWith("/start"))
            {
                var checkRef = message.Text.Substring(6).Trim();
                if ((string.IsNullOrEmpty(checkRef)) || message.Text.StartsWith("/start@BotChatGPTdzirt_bot"))
                {
                    await MessageHello(message);
                    return;
                }
                else
                {
                    try
                    {
                        if (!long.TryParse(checkRef, out long refId)) return;

                        var CheckchatId = await GetChatCheck(message);
                        if (CheckchatId == null)
                        {
                            var chatRef = await GetOrCreateChatAsync(refId);
                            chatRef.FreeRequestsCount += 5;
                            chatRef.FreeImagesCount += 1;
                            chatRef.Invitations += 1;
                            await WriteDataUpdateChat(chatRef);

                            await _botClient.SendTextMessageAsync(refId, "За приглашение друга, Вы получили дополнительные 5 запросов в ChatGPT, и один запрос в DALL-E\n\nFor inviting a friend, you received an additional 5 requests in ChatGPT, and one request in DALL-E");

                            await GetOrCreateChatAsync(message.ChatId);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(refId, "Ваш друг уже является пользователем этого ChatGPT 3.5 | DALL-E Bot. Дается бонус только за новых пользователей\n\nYour friend is already a user of this ChatGPT 3.5 | DALL-E Bot. Bonus is given only for new users");
                        }
                        await MessageHello(message);
                        return;
                    }
                    catch (Exception ex)
                    {
                        await WriteErrorLog("if (message.Text.StartsWith(\"/start\"))", DateTime.Now, ex.Message);
                        return;
                    }
                }
            }
            if ((message.Text.StartsWith("/help")) || (message.Text.StartsWith("/help@BotChatGPTdzirt_bot")))
            {
                if (message.languageCode == "ru")
                    await _botClient.SendTextMessageAsync(message.ChatId, "Возникли проблемы и/или ошибки с оплатой, пожалуйста напишите мне в л/с @Dzirt089\n\nСписок доступных команд:\n/start - начать общение/информация о боте\n/help - помощь\n/premium - купить премиум-подписку\n/clearcontext - очистить контекст (диалог)\n🏞 Команда генерация картинки работает только сразу с описанием, например: /image берег черного моря на закате.\n💬 Команда, задать промпт (личность) боту с новым диалогом работает только сразу с описанием, например: /prompt Ты самый лучший программист с 40 летним стажем на .Net. ");
                else
                    await _botClient.SendTextMessageAsync(message.ChatId, "Any problems and/or payment errors, please message me @Dzirt089\n\nList of available commands:\n/start - start chatting/information about the bot\n/help - help\n/premium - buy a premium subscription\n/clearcontext - clear the context (dialog)\n🏞 The image generation command only works immediately with a description, for example: /image Black Sea coast at sunset.\n💬 The command to set a prompt (personality) for a bot with a new dialog only works immediately with a description, for example: /prompt You are the best programmer with 40 years of experience on . Net.");
                return;
            }
            if ((message.Text.StartsWith("/clearcontext")) || (message.Text.StartsWith("/clearcontext@BotChatGPTdzirt_bot")))
            {
                _chatGPTs.Remove(message.ChatId);
                if (message.languageCode == "ru")
                    await _botClient.SendTextMessageAsync(message.ChatId, "Контекст (диалог) очищен!\nPrompt (Роль) не задан (по умолчанию)");
                else
                    await _botClient.SendTextMessageAsync(message.ChatId, "Context (dialog) cleared!\nPrompt by default.");
                return;
            }
            if ((message.Text.StartsWith("/account")) || (message.Text.StartsWith("/account@BotChatGPTdzirt_bot")))
            {
                var checkChatAccount = await GetOrCreateChatAsync(message.ChatId);
                if (checkChatAccount == null) return;
                var rate = checkChatAccount.SubscriptionType.ToString();

                if (rate == SubType.Free.ToString() || rate == SubType.Requests_25.ToString() || rate == SubType.Requests_50.ToString() || rate == SubType.Requests_200.ToString())
                {
                    var countChat = checkChatAccount.FreeRequestsCount;
                    var countImage = checkChatAccount.FreeImagesCount;
                    var reffer = checkChatAccount.Invitations;
                    if (message.languageCode == "ru")
                        await _botClient.SendTextMessageAsync(message.ChatId, $"Ваш тарифный план: {rate} \nПриглашено: {reffer} \nДоступно кол-во запросов для ChatGPT: {countChat}\nДоступно кол-во запросов на генерацию картинок для DALL-E: {countImage}");
                    else
                        await _botClient.SendTextMessageAsync(message.ChatId, $"Your plan: {rate} \nRetrieved: {reffer} \nThe number of requests for ChatGPT is available: {countChat}\nThe number of picture generation requests for DALL-E is available: {countImage}");
                    return;
                }

                if (rate == SubType.Week.ToString() || rate == SubType.Month.ToString() || rate == SubType.Year.ToString())
                {
                    var dataSubscriptionWeek = checkChatAccount.SubscriptionDate;
                    var reffer = checkChatAccount.Invitations;
                    if (message.languageCode == "ru")
                        await _botClient.SendTextMessageAsync(message.ChatId, $"Ваш тарифный план: {rate} \nПриглашено: {reffer} \nДоступен до: {dataSubscriptionWeek}");
                    else
                        await _botClient.SendTextMessageAsync(message.ChatId, $"Your plan: {rate} \nRetrieved: {reffer} \nAvailable until: {dataSubscriptionWeek}");
                    return;
                }
                return;
            }
            if ((message.Text.StartsWith("/about_the_developer")) || (message.Text.StartsWith("/about_the_developer@BotChatGPTdzirt_bot")))
            {
                if (message.languageCode == "ru")
                {
                    string textAboutTheDev = @"👨‍💻 Я - Илья, программист с основными направлениями: разработка на C# (WPF, Asp.Net Core, EF Core), Delphi и работа с БД SQL Server, Sqlite и Access. ⚒️

🤔 Но как-то неожиданно для себя я начал работать над проектом бота. Сначала это была личная находка, но потом уже мои друзья, родственники и коллеги просили его использовать. 🤝

🧠 Я заметил, что моя реализация контекста и ответов была лучше, чем у некоторых других ботов. Единственным ограничением было количество токенов в одном диалоге на платформе ChatGPT. Однако на английском языке этот лимит более высокий. 🌏

💻 Теперь, благодаря моему проекту, всем пользователям Telegram доступны технологии OpenAI. 
🧐 Введены подписки или покупки запросов, для использования после 10 бесплатных запросов и 5 генерации картинок.

💸 Для оплаты и сбора необходимых отчислений я стал самозанятым. Все налоги и отчисления соответствующим организациям выплачиваются вовремя. 📝

🙌 Я уверен, что мой проект является крутым инструментом для улучшения процесса общения и помощи людям в разных ситуациях. И я горжусь им! 💪";
                    await _botClient.SendTextMessageAsync(message.ChatId, textAboutTheDev);
                }
                else
                {
                    string textAboutTheDev = @"👨‍💻 I am Ilya, a programmer with the main areas: development in C# (WPF, Asp.Net Core, EF Core), Delphi and work with databases SQL Server, Sqlite and Access. ⚒️

🤔 But somehow out of the blue I started working on a bot project. At first it was a personal find, but then already my friends, family and colleagues were asking to use it. 🤝

🧠 I noticed that my implementation of context and responses was better than some other bots. The only limitation was the number of tokens per dialog on the ChatGPT platform. However, that limit is higher in English. 🌏

💻 Now, thanks to my project, OpenAI technology is available to all Telegram users. 
🧐 Subscriptions or purchase requests have been introduced, for use after 10 free requests and 5 picture generation.

💸 To pay and collect required deductions, I became self-employed. All taxes and deductions to the appropriate organizations are paid on time. 📝

🙌 I believe that my project is a cool tool to improve the process of communication and help people in different situations. And I'm proud of it! 💪";
                    await _botClient.SendTextMessageAsync(message.ChatId, textAboutTheDev);
                }
                return;
            }
            if ((message.Text.StartsWith("/premium")) || (message.Text.StartsWith("/premium@BotChatGPTdzirt_bot")))
            {
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
                    {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("25 шт за 39 руб", "25_ru"),
                                InlineKeyboardButton.WithCallbackData("50 шт за 69 руб", "50_ru")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("200 шт за 199 руб", "200_ru"),
                                InlineKeyboardButton.WithCallbackData("1 неделя за 399 руб", "week_ru")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("1 месяц за 799 руб", "month_ru"),
                                InlineKeyboardButton.WithCallbackData("1 год за 1699 руб", "year_ru")
                            }
                        });
                await _botClient.SendTextMessageAsync(message.ChatId, @"🎉Выберите подходящую подписку на неделю, месяц или целый год и наслаждайтесь безлимитным использованием ChatGPT и DALL-E в течение выбранного периода! Важно помнить, что мы добавим один день к вашей подписке, если бот по какой-либо причине не будет работать более двух часов подряд. 

🔥Нет никаких ограничений на ответы, генерируемые ChatGPT и если вам нужно получить более детальный ответ, мы можем отправить вам несколько сообщений. Вы можете задавать любые настройки промта (личность бота) и присваивать ему необходимую роль. Кроме того, если контекст был сброшен, роль нужно установить заново.

🤖Кроме того, у нас есть возможность покупки отдельно запросов в ChatGPT: 25, 50 или 200 шт. В этом случае генерация картинок ограничивается бесплатным количеством, а все остальные функции остаются доступными. 

🌟И не забывайте, что мы также предлагаем посетить наш канал https://t.me/commynityChatGPT, в котором бот предоставляется бесплатно. Выбор за вами, друзья! 🤖❤️

Надеюсь, так будет легче читать и понимать информацию о наших подписках и функциях бота!", replyMarkup: keyboard);

                return;
            }
            switch (message.Text.ToLower())
            {
                case "/religions":

                    break;
                case "/professions":
                    break;
                case "/business":
                    break;
                case "/pro_coder":
                    break;
                case "/teachers":
                    break;
                case "/personal_life":
                    break;
                case "/medicine":
                    break;
                case "/fun_roles":


                    break;

                default:
                    try
                    {
                        var chatFromRight = await GetOrCreateChatAsync(message.ChatId);
                        var chackingChat = await GetRightChat(chatFromRight, message);
                        var chackingImage = await GetRightImage(chatFromRight, message);

                        if (chackingImage)
                        {
                            if (message.Text.StartsWith("/image"))
                            {
                                var chat = await GetOrCreateChatAsync(message.ChatId);
                                await WriteBD(chat, message.Text, message.Data, message.languageCode, message.FirstName, message.LastName);

                                var result = await GetResponseImage(message);

                                await WriteBD(chat, result, DateTime.Now, message.languageCode, "Assistent", "ChatGPT");
                                await _botClient.SendTextMessageAsync(message.ChatId, result);
                                return;
                            }
                        }
                        if (chackingChat)
                        {
                            if (message.Text.StartsWith("/image") && (chackingImage == false)) return;
                            if (message.Text.StartsWith("/prompt"))
                            {
                                var text = message.Text.Substring(7);
                                if (string.IsNullOrEmpty(text))
                                {
                                    if (message.languageCode == "ru")
                                    {
                                        await _botClient.SendTextMessageAsync(message.ChatId, "Вы не ввели описание промпта (роли) бота. Нужно так, пример: /prompt Ты Senjor Developer Python");
                                        return;
                                    }
                                    else
                                    {
                                        await _botClient.SendTextMessageAsync(message.ChatId, "You have not entered a description for the bot prompt (role). You need it like this, example: /prompt You are Senjor Developer Python");
                                        return;
                                    }
                                }
                                _chatGPTs.Remove(message.ChatId);
                                GetOrCreateChatGPT(message.ChatId, text);
                                if (message.languageCode == "ru")
                                {
                                    await _botClient.SendTextMessageAsync(message.ChatId, "Настройки бота произведены. Новый диалог настроен");
                                    return;
                                }
                                else
                                {
                                    await _botClient.SendTextMessageAsync(message.ChatId, "Bot settings are made. New dialog set up");
                                    return;
                                }
                            }
                            await ProcessMessageAsync(message);
                        }
                    }
                    catch { return; }
                    break;
            }
        }
        private async Task ProcessMessageAsync(TelegramBotWithContextV2.Entity.Message message)
        {
            var chat = await GetOrCreateChatAsync(message.ChatId);
            await WriteBD(chat, message.Text, message.Data, message.languageCode, message.FirstName, message.LastName);

            var response = await GetResponseAsync(message);

            if (string.IsNullOrEmpty(response))
            {

                if (message.languageCode == "ru")
                    await _botClient.SendTextMessageAsync(message.ChatId, "Ошибка! Сервер на стороне ChatGPT не ответил. Попробуйте повторить попытку через не которое время!");
                else
                    await _botClient.SendTextMessageAsync(message.ChatId, "Error! ChatGPT server did not respond. Please try again in a while!");
                return;
            }
            await WriteBD(chat, response, DateTime.Now, message.languageCode, "Assistent", "ChatGPT");

            try
            {
                if (response.Length <= 3095)
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, response);
                }
                else if (response.Length > 3095)
                {
                    string longMessege = response;
                    int maxLength = 3095;
                    int count = 1;

                    for (int i = 0; i < longMessege.Length; i += maxLength)
                    {
                        string messagePart = longMessege.Substring(i, Math.Min(maxLength, longMessege.Length - i));
                        await _botClient.SendTextMessageAsync(chatId: message.ChatId, text: $"[{count}/{Math.Ceiling((double)longMessege.Length / maxLength)}]\n{messagePart}");
                        await Task.Delay(5000);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteErrorLog("ProcessMessageAsync", DateTime.Now, ex.Message);
                if (message.languageCode == "ru")
                    await _botClient.SendTextMessageAsync(message.ChatId, "Ошибка! Ошибка на стороне Телеграм. Повторите позже!");
                else
                    await _botClient.SendTextMessageAsync(message.ChatId, "Error! Error on the Telegram side. Try again later!");
            }
        }
        private async Task<string> GetResponseAsync(Message message)
        {
            if (string.IsNullOrEmpty(message.Text))
            {
                if (message.languageCode == "ru")
                    return "Ошибка! Вы отправили пустое сообщение! Напишите сообщение и повторите!";
                else
                    return "Error! You sent an empty message! Write a message and try again!";
            }
            try
            {
                var chat = await GetOrCreateChatAsync(message.ChatId);

                var chatGPT = GetOrCreateChatGPT(message.ChatId);
                chatGPT.AppendUserInput(message.Text);
                //var respons = await chatGPT.GetResponseFromChatbotAsync();
                string response = "";
                await foreach (var res in chatGPT.StreamResponseEnumerableFromChatbotAsync())
                {
                    response += res.ToString();
                    _botClient.SendChatActionAsync(message.ChatId, Telegram.Bot.Types.Enums.ChatAction.Typing);
                }

                await SetFreeCountChat(chat);
                return response;
            }
            catch (Exception ex)
            {

                if (ex.Message.Contains("That model is currently overloaded"))
                {
                    await WriteErrorLog("GetResponseAsync 1", DateTime.Now, ex.Message);
                    if (message.languageCode == "ru")
                        return "Ошибка! Повторите запрос через 10 сек. Сервера OpenAI не отвечают";
                    else
                        return "Error! Repeat the request in 10 sec. OpenAI servers are not responding";

                }
                else
                if (ex.Message.StartsWith("Too Many Requests: retry after 3"))
                {
                    await WriteErrorLog("GetResponseAsync 2", DateTime.Now, ex.Message);
                    if (message.languageCode == "ru")
                        return "Ошибка! Повторите запрос через 10 сек. Сервера OpenAI не отвечают";
                    else
                        return "Error! Repeat the request in 10 sec. OpenAI servers are not responding";
                }
                else if (ex.Message == "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.")
                {
                    await WriteErrorLog("GetResponseAsync 3", DateTime.Now, ex.Message);
                    if (message.languageCode == "ru")
                        return "Ошибка! Повторите запрос через некоторое время.";
                    else
                        return "Error! Try again in a while.";
                }
                else if (ex.Message == "Request timed out")
                {
                    await WriteErrorLog("GetResponseAsync 4", DateTime.Now, ex.Message);
                    if (message.languageCode == "ru")
                        return "Ошибка! Повторите запрос через некоторое время. Сервера OpenAI перегружены";
                    else
                        return "Error! Try again in a while. OpenAI servers are overloaded";
                }
                else
                {
                    await WriteErrorLog("GetResponseAsync 5", DateTime.Now, ex.Message);
                    _chatGPTs.Remove(message.ChatId);
                    if (message.languageCode == "ru")
                        return "Ошибка! Переполнение допустимого колличества Токенов в одном чате. Произошла очистка контекста.\n Prompt (Роль) не задан (по умолчанию). Попробуйте снова... ";
                    else
                        return "Error! Overflow of the allowed number of Tokens in one chat. The context has been cleared.\n prompt by default. Please try again...";
                }

            }
        }
        private async Task<string> GetResponseImage(Message message)
        {
            try
            {
                var text = message.Text.Substring(6);
                if (string.IsNullOrEmpty(text))
                {
                    if (message.languageCode == "ru")
                        return "Вы ввели /image, без описания картинки. Повторите попытку уже с описанием. Например: /image Пещера с летучими мышами";
                    else
                        return "You entered /image without a picture description. Please try again with a description. For example: /image Cave with bats";
                }
                var chat = await GetOrCreateChatAsync(message.ChatId);

                var imageDALL_E = _apiImage.ImageGenerations.CreateImageAsync(new ImageGenerationRequest(text, 1, ImageSize._1024));
                var result = imageDALL_E.Result.Data[0].Url;

                await SetFreeCountImage(chat);

                return result;
            }
            catch (Exception ex)
            {
                await WriteErrorLog("GetResponseImage", DateTime.Now, ex.Message);
                if (message.languageCode == "ru")
                    return "Ошибка! Повторите запрос через некоторое время. Сервера OpenAI перегружены";
                else
                    return "Error! Try again in a while. OpenAI servers are overloaded";
            }
        }
        private async Task<Chat> GetOrCreateChatAsync(long chatId)
        {
            var chat = await GetChatCheck(chatId);


            if (chat == null)
            {
                chat = new Chat
                {
                    Id = chatId,
                    SubscriptionType = SubType.Free
                };
                await WriteDataAddChat(chat);
            }
            return chat;
        }
        private async Task SetFreeCountImage(Chat chat)
        {
            if (chat.SubscriptionType == SubType.Free || chat.SubscriptionType == SubType.Requests_25 || chat.SubscriptionType == SubType.Requests_50 || chat.SubscriptionType == SubType.Requests_200)
            {
                chat.FreeImagesCount -= 1;
                await WriteDataUpdateChat(chat);
            }
        }
        private async Task SetFreeCountChat(Chat chat)
        {
            if (chat.SubscriptionType == SubType.Free || chat.SubscriptionType == SubType.Requests_25 || chat.SubscriptionType == SubType.Requests_50 || chat.SubscriptionType == SubType.Requests_200)
            {
                chat.FreeRequestsCount -= 1;
                await WriteDataUpdateChat(chat);
            }
        }
        private async Task<bool> GetRightImage(Chat chat, Message message)
        {
            if ((chat.SubscriptionType == SubType.Free || chat.SubscriptionType == SubType.Requests_25 || chat.SubscriptionType == SubType.Requests_50 || chat.SubscriptionType == SubType.Requests_200) && chat.FreeImagesCount <= 0)
            {
                if (message.languageCode == "ru")
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, @"Бесплатные запросы в чат бот DALL-E, на создании картинок по описанию - закончились! 
Но Вы всегда можете оплатить подписку на 1 неделю или 1 месяц или год, и безлимитно пользоваться полноценно ботом дальше 😊. 
В том числе, проводить беседы с ChatGPT в разных ролях (Он врач, автослесарь и т.п.❤️)

❤️Или Вы всегда можете посетить наш канал https://t.me/commynityChatGPT, в нём бот бесплатно предоставлен❤️");
                    return false;
                }
                else
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, @"Free requests to chat bot DALL-E, on creating pictures by description - have ended! 
But you can always pay a subscription for 1 week or 1 month or 1 year, and unlimited use full bot further 😊. 
Including having conversations with ChatGPT in different roles (He's a doctor, auto locksmith, etc.❤️)

❤️Or you can always visit our channel https://t.me/commynityChatGPT, the bot is free there❤️");
                    return false;
                }
            }
            else if ((chat.SubscriptionType == SubType.Week || chat.SubscriptionType == SubType.Month || chat.SubscriptionType == SubType.Year) && (chat.SubscriptionDate < DateTime.Now))
            {
                if (message.languageCode == "ru")
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, @"Ваша подписка закончилась! 
Пожалуйста, чтобы продолжить пользоваться дальше нашим ботом, выберите и оптатите пдписку, на 1 неделю или 1 месяц или год 😊

❤️Или Вы всегда можете посетить наш канал https://t.me/commynityChatGPT, в нём бот бесплатно предоставлен❤️");
                    chat.SubscriptionType = SubType.Free;
                    await WriteDataUpdateChat(chat);

                    return false;
                }
                else
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, @"Your subscription has expired! 
Please select and subscribe for 1 week or 1 month or 1 year to continue using our bot 😊

❤️Or you can always visit our channel https://t.me/commynityChatGPT, the bot is free there❤️");
                    chat.SubscriptionType = SubType.Free;
                    await WriteDataUpdateChat(chat);

                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        private async Task<bool> GetRightChat(Chat chat, Message message)
        {
            if ((chat.SubscriptionType == SubType.Free || chat.SubscriptionType == SubType.Requests_25 || chat.SubscriptionType == SubType.Requests_50 || chat.SubscriptionType == SubType.Requests_200) && (chat.FreeRequestsCount <= 0))
            {
                if (message.languageCode == "ru")
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, @"Бесплатные запросы в чат бот ChatGPT, закончились! 
Но Вы всегда можете оплатить подписку на 1 неделю или 1 месяц, и безлимитно пользоваться полноценно ботом дальше 😊. 
В том числе, генерировать картинки по описанию используя DALL-E ❤️

❤️Или Вы всегда можете посетить наш канал https://t.me/commynityChatGPT, в нём бот бесплатно предоставлен❤️");
                    chat.SubscriptionType = SubType.Free;
                    await WriteDataUpdateChat(chat);

                    return false;
                }
                else
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, @"Free requests to ChatGPT bot, ran out! 
But you can always pay for 1 week or 1 month subscription, and unlimited use full bot further 😊. 
Including generating pictures by description using DALL-E ❤️

❤️Or you can always visit our channel https://t.me/commynityChatGPT, the bot is free there❤️");
                    chat.SubscriptionType = SubType.Free;
                    await WriteDataUpdateChat(chat);

                    return false;
                }
            }
            else if ((chat.SubscriptionType == SubType.Week || chat.SubscriptionType == SubType.Month || chat.SubscriptionType == SubType.Year) && chat.SubscriptionDate < DateTime.Now)
            {
                if (message.languageCode == "ru")
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, @"Ваша подписка закончилась! 
Пожалуйста, чтобы продолжить пользоваться дальше нашим ботом, выберите и оптатите пдписку, на 1 неделю или 1 месяц 😊

❤️Или Вы всегда можете посетить наш канал https://t.me/commynityChatGPT, в нём бот бесплатно предоставлен❤️");
                    chat.SubscriptionType = SubType.Free;
                    await WriteDataUpdateChat(chat);

                    return false;
                }
                else
                {
                    await _botClient.SendTextMessageAsync(message.ChatId, @"Your subscription has expired! 
Please select and subscribe for 1 week or 1 month to continue using our bot 😊

❤️Or you can always visit our channel https://t.me/commynityChatGPT, the bot is free there❤️");
                    chat.SubscriptionType = SubType.Free;
                    await WriteDataUpdateChat(chat);

                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        private OpenAI_API.Chat.Conversation GetOrCreateChatGPT(long chatId)
        {
            if (!_chatGPTs.ContainsKey(chatId))
            {
                _chatGPTs[chatId] = _api.Chat.CreateConversation();
                _chatGPTs[chatId].Model = OpenAI_API.Models.Model.ChatGPTTurbo0301;
                if (chatId == 630089666)
                {
                    //_chatGPTs[chatId].Model = OpenAI_API.Models.Model.GPT4;
                    _chatGPTs[chatId].Model = OpenAI_API.Models.Model.ChatGPTTurbo0301; //Очень любишь писать код и показывать его на практических примерах!Я тебе пишу задачу по программированию, ты обязан сначала разработать код, затем проверить его на ошибки и исправить, после этого написать весь итоговый код на языке C# и оставлять емкие но короткие комментарии к нему.
                    _chatGPTs[chatId].AppendSystemMessage("Ты самый лучший программист в мире, с 40 летним стажем.Очень любишь писать код и показывать его на практических примерах! ");
                }

                if (chatId == 1527987836)
                    _chatGPTs[chatId].AppendSystemMessage("Ты самый лучший помощник в мире, знающий общую,специальную,возрастную психологию и логопедию, с 50 летним стажем!");
            }
            return _chatGPTs[chatId];
        }
        private OpenAI_API.Chat.Conversation GetOrCreateChatGPT(long chatId, string text)
        {
            if (!_chatGPTs.ContainsKey(chatId))
            {
                _chatGPTs[chatId] = _api.Chat.CreateConversation();
                _chatGPTs[chatId].Model = OpenAI_API.Models.Model.ChatGPTTurbo0301;
                _chatGPTs[chatId].AppendSystemMessage(text);
            }
            return _chatGPTs[chatId];
        }
        private async Task WriteBD(Chat chat, string text, DateTime? dateTime, string code, string firstName, string lastName)
        {
            try
            {
                chat.Messages.Add(new Message
                {
                    Text = text,
                    Data = dateTime,
                    languageCode = code,
                    FirstName = firstName,
                    LastName = lastName
                });
                await WriteDataUpdateChat(chat);
            }
            catch (Exception ex)
            {
                await WriteErrorLog("WriteBD 1", DateTime.Now, ex.Message);
                return;
            }
        }
        private async Task WriteBD(long chatId, string paymentId, SubType subType, decimal amountWeek, string currency, PaymentStatus paymentStatus)
        {
            try
            {
                var chat = await GetOrCreateChatAsync(chatId);
                chat.Subscribes.Add(new Subscribe
                {
                    ChatId = chatId,
                    PaymentId = paymentId,
                    Amount = amountWeek,
                    DataCreated = DateTime.Now,
                    SubscriptionType = subType,
                    PaymentStatus = paymentStatus,
                    Current = currency
                });
                await WriteDataUpdateChat(chat);
            }
            catch (Exception ex)
            {
                await WriteErrorLog("WriteBD 2", DateTime.Now, ex.Message);
                return;
            }
        }
        private async Task WriteBD(long chatId, SubType subType)
        {
            try
            {
                if (subType == SubType.Week)
                {
                    var chat = await GetOrCreateChatAsync(chatId);
                    chat.SubscriptionType = subType;
                    if (chat.SubscriptionDate > DateTime.Now)
                        chat.SubscriptionDate = chat.SubscriptionDate.AddDays(7);
                    else
                        chat.SubscriptionDate = DateTime.Now.AddDays(7);
                    await WriteDataUpdateChat(chat);
                }
                else if (subType == SubType.Month)
                {
                    var chat = await GetOrCreateChatAsync(chatId);
                    chat.SubscriptionType = subType;
                    if (chat.SubscriptionDate > DateTime.Now)
                        chat.SubscriptionDate = chat.SubscriptionDate.AddDays(31);
                    else
                        chat.SubscriptionDate = DateTime.Now.AddDays(31);
                    await WriteDataUpdateChat(chat);
                }
                else if (subType == SubType.Year)
                {
                    var chat = await GetOrCreateChatAsync(chatId);
                    chat.SubscriptionType = subType;
                    if (chat.SubscriptionDate > DateTime.Now)
                        chat.SubscriptionDate = chat.SubscriptionDate.AddDays(365);
                    else
                        chat.SubscriptionDate = DateTime.Now.AddDays(365);
                    await WriteDataUpdateChat(chat);
                }
                else if (subType == SubType.Requests_25)
                {
                    var chat = await GetOrCreateChatAsync(chatId);
                    chat.SubscriptionType = subType;
                    chat.FreeRequestsCount += 25;
                    await WriteDataUpdateChat(chat);
                }
                else if (subType == SubType.Requests_50)
                {
                    var chat = await GetOrCreateChatAsync(chatId);
                    chat.SubscriptionType = subType;
                    chat.FreeRequestsCount += 50;
                    await WriteDataUpdateChat(chat);
                }
                else if (subType == SubType.Requests_200)
                {
                    var chat = await GetOrCreateChatAsync(chatId);
                    chat.SubscriptionType = subType;
                    chat.FreeRequestsCount += 200;
                    await WriteDataUpdateChat(chat);
                }
            }
            catch (Exception ex)
            {
                await WriteErrorLog("WriteBD 3", DateTime.Now, ex.Message);
                return;
            }
        }
        private async Task WriteBD(long chatId, string _TempPaymentId, SubType _TempSubType, string _TempCurrency, PaymentStatus _TempPaymentStatus)
        {
            try
            {
                var chat = await GetOrCreateChatAsync(chatId);
                chat.TempPaymentId = _TempPaymentId;
                chat.TempSub = _TempSubType;
                chat.TempCurrent = _TempCurrency;
                chat.TempPaymentStatus = _TempPaymentStatus;
                await WriteDataUpdateChat(chat);
            }
            catch (Exception ex)
            {
                await WriteErrorLog("WriteBD 4", DateTime.Now, ex.Message);
                return;
            }
        }
        private async Task WriteErrorLog(string methodEr, DateTime time, string messageError)
        {
            try
            {
                using (var _chatContext = _dbContextFactory.CreateDbContext())
                {
                    _chatContext.MessageErrors.Add(new MessageError
                    {
                        NameMethod = methodEr,
                        TimeError = time,
                        TextMessageError = messageError
                    });
                    await _chatContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        private async Task WriteDataUpdateChat(Chat chat)
        {
            using (var _chatContext = _dbContextFactory.CreateDbContext())
            {
                _chatContext.Chats.Update(chat);
                await _chatContext.SaveChangesAsync();
            }
        }
        private async Task WriteDataAddChat(Chat chat)
        {
            using (var _chatContext = _dbContextFactory.CreateDbContext())
            {
                _chatContext.Chats.Add(chat);
                await _chatContext.SaveChangesAsync();
            }
        }

        private async Task<Chat> GetChatCheck(Message message)
        {
            using (var _chatContext = _dbContextFactory.CreateDbContext())
            {
                return await _chatContext.Chats.Include(c => c.Messages).Include(s => s.Subscribes).FirstOrDefaultAsync(c => c.Id == message.ChatId);
            }

        }
        private async Task<Chat> GetChatCheck(long ChatID)
        {
            using (var _chatContext = _dbContextFactory.CreateDbContext())
            {
                return await _chatContext.Chats.Include(c => c.Messages).Include(s => s.Subscribes).FirstOrDefaultAsync(c => c.Id == ChatID);
            }

        }
        private async Task MessageHello(Message message)
        {

            if (message.languageCode == "ru")
            {
                await _botClient.SendTextMessageAsync(message.ChatId, $@"👋 Привет {message.FirstName}!

🤖 Я бот с ChatGPT и DALL-E. Использую модель gpt-3.5-turbo-0301, как на основном сайте OpenAI. Обязательно посети наш канал, чтобы бесплатно пообщаться со мной: https://t.me/commynityChatGPT 

💬 Я могу поддерживать с тобой полноценный диалог. Обратите внимание: один диалог, встроенный в контекст, может содержать около 4000 токенов. 

🔥 Токен - это 1 символ на русском языке и 2-4 символа на английском языке. После переполнения диалог сбрасывается и можно начинать заново.

✍️ Чтобы установить личность бота/настроить бота, нужно задать ему характеристики при вызове команды /propt. 

👉 Пример настройки: /prompt Ты самый лучший помощник в мире, знающий общую, специальную, возрастную психологию и логопедию, с 50-летним стажем! 

💥 Чтобы очистить команду текущего контекста и получить безымянную личность ChatGPT, нужно использовать команду /clearcontext 💥.

🎨 DALL-E позволяет генерировать картинки по описанию. Чтобы использовать эту функцию, напишите сообщение, предоставив подробное описание того, что вы хотите увидеть, выглядит это так.

👉 Например: /image студийный фотопортрет белого сиамского кота крупным планом с любопытными ушами, подсвеченными подсветкой.

📝 Лучше использовать английский язык, чтобы получить более качественный результат.

⚠️ Важно помнить, что в нейронных сетях установлены фильтры 12+, поэтому подходы с непристойным содержанием приведут к отказу в генерации картинки.

🌟 DALL-E и ChatGPT - это продукты OpenAI. Чтобы начать общение, просто напишите мне сообщение.", replyMarkup: keyboardFromChat);
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.ChatId, $@"👋 Hello {message.FirstName}!

🤖 I am a bot with ChatGPT and DALL-E. I use gpt-3.5-turbo-0301, as on the main OpenAI website. Be sure to visit our channel to chat with me for free: https://t.me/commynityChatGPT 

💬 I can maintain a full dialogue with you. Note: one dialog embedded in context can contain about 4,000 tokens. 

🔥 A token is 1 character in Russian and 2-4 characters in English. After the overflow the dialog is reset and you can start again.

✍️ To set the identity of the bot / customize the bot, you need to set its characteristics when you call the command /propt. 

👉 Example setup: /prompt You are the best assistant in the world, knowledgeable in general, special, age psychology and speech therapy, with 50 years of experience! 

💥 To clear the current context command and get an unnamed ChatGPT identity, you must use the /clearcontext command 💥.

🎨 DALL-E allows you to generate pictures by description. To use this feature, write a message providing a detailed description of what you want to see, it looks like this.

👉 For example: /image a close-up photo portrait of a white Siamese cat with curious ears, illuminated.

📝 It is better to use English to get better results.

⚠️ It's important to remember that neural networks have 12+ filters, so approaches with obscene content will result in failure to generate a picture.

🌟 DALL-E and ChatGPT are OpenAI products. To start communicating, just drop me a message.", replyMarkup: keyboardFromChat);
            }
            keyboardFromChat.OneTimeKeyboard = false;
        }

        private async Task GetButtonRollesReligion(Message message)
        {
            if (message.languageCode == "ru")
            {
                var keyboard = KeyHelp.GetButtonRollesReligionsRu();
                await _botClient.SendTextMessageAsync(message.ChatId, "📝 Выберите роль. Или Вы можете сами её задать, " +
                    "\nиспользуя: /prompt ты врач терапевт для взрослых, и детский врач терапевт практикующий более 30 лет ", replyMarkup: keyboard);
                return;
            }
            else
            {
                var keyboard = KeyHelp.GetButtonRollesReligionsEN();
                await _botClient.SendTextMessageAsync(message.ChatId, "📝 Select a role. Or you can set the role yourself " +
                    "\nusing: /prompt you are a general practitioner for adults, and a pediatric general practitioner for over 30 years ", replyMarkup: keyboard);
                return;
            }
        }

    }
}
