using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks; 
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Drawing;


namespace NeuralNetwork1
{
    class TLGBotik
    {
        public List<string> PossibleAnswers = new List<string>
        {
            "Я думаю, что это {0}. Надеюсь, угадал",
            "Мне кажется, что это {0}. Рисуешь ты, конечно...\n\n\n\n...классно",
            "Наверное, это {0}. Ну, по крайней мере, я так думаю",
            "Смотри, планет много, а я один, так что не принимай на веру: вроде, это {0}",
            "Ага, ну это {0}. По идее.",
            "Хм... Не просто. {0}, да?",
            "{0}. Должно быть, так",
            "Мои нейроны, конечно, пропотели, пока считали это, но, вроде, результат есть. Это {0}",
            "Секунду, монетку подброшу и скажу...\n\nДа шучу я! Монетки бросают только дилетанты. У нас кубик...\n\nВ общем, это {0}. Надеюсь, не ошибся",
            "По идее, {0}",
            "Это {0}",
            "Вроде как, {0}",
            "Пам-пам-пам... {0}?",
            "Полагаю, что {0}"
        };

        public Telegram.Bot.TelegramBotClient botik = null;
        public AIMLBotik AIMLbot = null;
        public Random random = new Random();

        private UpdateTLGMessages formUpdater;
        private UpdateImageData dataUpdater;

        private BaseNetwork perseptron = null;
        // CancellationToken - инструмент для отмены задач, запущенных в отдельном потоке
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        public TLGBotik(BaseNetwork net, UpdateTLGMessages updater, UpdateImageData data_updater)
        { 
            var botKey = System.IO.File.ReadAllText("botkey.txt");
            AIMLbot = new AIMLBotik();
            botik = new Telegram.Bot.TelegramBotClient(botKey);
            formUpdater = updater;
            dataUpdater = data_updater;
            perseptron = net;
        }

        public void SetNet(BaseNetwork net)
        {
            perseptron = net;
            formUpdater("Net updated!");
        }

        private async Task HandleUpdateMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;
            formUpdater("Тип сообщения : " + message.Type.ToString());
            string chat_id = message.Chat.Id.ToString();
            if (!AIMLbot.Users.ContainsKey(chat_id))
            {
                var name = message.Chat.FirstName;
                AIMLbot.AddUser(chat_id, name);
                AIMLbot.Dump();
                var answer = AIMLbot.Talk(chat_id, $"Я НОВЕНЬКИЙ {name}");
                botik.SendTextMessageAsync(message.Chat.Id, answer);
                return;
            }
            var user = AIMLbot.Users[chat_id];
            AIMLbot.Talk(user.Id, $"ОБНОВЛЕНИЕ ИМЕНИ {user.Name}");
            AIMLbot.Talk(user.Id, $"ОБНОВЛЕНИЕ ФИГУРЫ {Info.figureToStr[user.LastFigure]}");

            if (message.Type == MessageType.Photo)
            {
                formUpdater("Picture loadining started");
                var photoId = message.Photo.Last().FileId;
                Telegram.Bot.Types.File fl = botik.GetFileAsync(photoId).Result;
                var imageStream = new MemoryStream();
                await botik.DownloadFileAsync(fl.FilePath, imageStream, cancellationToken: cancellationToken);
                var img = System.Drawing.Image.FromStream(imageStream);
                System.Drawing.Bitmap bm = new System.Drawing.Bitmap(img);
                var size = new System.Drawing.Size(Info.Width, Info.Height);
                var cropped = ImageEditor.CropToSquare(bm);
                var bw = ImageEditor.RGBtoBW(cropped);
                var resized = ImageEditor.SmoothResize(bw, size);
                var sharpened = ImageEditor.SharpenBW(resized, 220);
                var normalized = ImageEditor.NormalizeByMassCenter(sharpened, size);
                var sharpened_normalized = ImageEditor.SharpenBW(normalized, 210);
                var sample = new Sample(ImageEditor.GetArray(sharpened_normalized), Info.ClassesNum);
                var fig = perseptron.Predict(sample);
                dataUpdater(sharpened_normalized, cropped, String.Format("{0} ({1:0.00}%)", Info.figureToStr[fig], perseptron.LastAccuracy * 100));
                user.LastFigure = fig;
                AIMLbot.Dump();
                string full_answer = String.Format(PossibleAnswers[random.Next(0, PossibleAnswers.Count)], Info.figureToStr[fig]);
                botik.SendTextMessageAsync(message.Chat.Id, full_answer);
                return;
            }

            if (message.Type == MessageType.Text)
            {
                var answer = AIMLbot.Talk(user.Id, message.Text);
                botik.SendTextMessageAsync(long.Parse(user.Id), answer);
                return;
            }

            botik.SendTextMessageAsync(long.Parse(user.Id), "Ох, что-то сложное ты прислал... Я понимаю только букавки и фотачки. Можно их?");
            return;
        }
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var apiRequestException = exception as ApiRequestException;
            if (apiRequestException != null)
                Console.WriteLine($"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}");
            else
                Console.WriteLine(exception.ToString());
            return Task.CompletedTask;
        }

        public bool Act()
        {
            try
            {
                botik.StartReceiving(HandleUpdateMessageAsync, HandleErrorAsync, new ReceiverOptions
                {   // Подписываемся только на сообщения
                    AllowedUpdates = new[] { UpdateType.Message }
                },
                cancellationToken: cts.Token);
                // Пробуем получить логин бота - тестируем соединение и токен
                Console.WriteLine($"Connected as {botik.GetMeAsync().Result}");
            }
            catch(Exception e) { 
                return false;
            }
            return true;
        }

        public void Stop()
        {
            cts.Cancel();
        }

    }
}
