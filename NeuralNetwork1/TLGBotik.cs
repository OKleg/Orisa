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
using AForge.Imaging;

namespace NeuralNetwork1
{
	class TLGBotik
	{   
		public AIMLBotik AIMLbot = null;
		public Telegram.Bot.TelegramBotClient botik = null;
		
		 
		private UpdateTLGMessages formUpdater;
		//...


		private BaseNetwork perseptron = null;
		// CancellationToken - инструмент для отмены задач, запущенных в отдельном потоке
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		public TLGBotik(BaseNetwork net,  UpdateTLGMessages updater)
		{
			AIMLbot = new AIMLBotik();
			var botKey = System.IO.File.ReadAllText("botkey.txt");
			botik = new Telegram.Bot.TelegramBotClient(botKey);
			formUpdater = updater;
			perseptron = net;
		}
		public Random random = new Random();
		public void SetNet(BaseNetwork net)
		{
			perseptron = net;
			formUpdater("Net updated!");
			formUpdater("https://t.me/OrisaBot");
		}

		private async Task HandleUpdateMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
		{
			//  Тут очень простое дело - банально отправляем назад сообщения
			var message = update.Message;
			formUpdater("Тип сообщения : " + message.Type.ToString());
			string chat_id = message.Chat.Id.ToString();
			if (!AIMLbot.Users.ContainsKey(chat_id))
			{
				var name = message.Chat.FirstName;
				AIMLbot.AddUser(chat_id, name);
				AIMLbot.Dump();
				var answer = AIMLbot.Talk(chat_id, $"СТАРТ {name}");
				await botik.SendTextMessageAsync(message.Chat.Id, answer);
				return;
			}
			
			formUpdater("Тип сообщения : " + message.Type.ToString());
			var user = AIMLbot.Users[chat_id];
			AIMLbot.Talk(user.Id, $"ИМЯ {user.Name}");
			AIMLbot.Talk(user.Id, $"НОВЫЙ ОБРАЗ {user.LastFigure}");
			
			//  Получение файла (картинки)
			if (message.Type == Telegram.Bot.Types.Enums.MessageType.Photo)
			{
				formUpdater("Picture loadining started");
				var photoId = message.Photo.Last().FileId;
				Telegram.Bot.Types.File fl = botik.GetFileAsync(photoId).Result;
				var imageStream = new MemoryStream();
				await botik.DownloadFileAsync(fl.FilePath, imageStream, cancellationToken: cancellationToken);
				var img = System.Drawing.Image.FromStream(imageStream);

				//Тут обработка изображения Артема(зачеркнуто) меня

				var bm = FilterImage.Filter(img);

				using (MemoryStream ms = new MemoryStream())
				{
					bm.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
					botik.SendPhotoAsync(message.Chat.Id, ms);
				}

				
				var fig = perseptron.Predict(new GenerateImage().GenerateFigure(bm));
				user.LastFigure = fig;
				switch (fig)
				{
					case FigureType.cpp: await botik.SendTextMessageAsync(message.Chat.Id, "Это прекрасный C++?");break;
					case FigureType.cs: await botik.SendTextMessageAsync(message.Chat.Id, "Это удобный C#?"); break;
					case FigureType.python: await botik.SendTextMessageAsync(message.Chat.Id, "Это доступный каждому Python?"); break;
					case FigureType.go: await botik.SendTextMessageAsync(message.Chat.Id, "Это Golang?"); break;
					case FigureType.java: await botik.SendTextMessageAsync(message.Chat.Id, "Это Java?"); break;
					case FigureType.js: await botik.SendTextMessageAsync(message.Chat.Id, "Это JS?"); break;
					case FigureType.haskell: await botik.SendTextMessageAsync(message.Chat.Id, "Это непонятный мне Haskel?"); break;
					case FigureType.pascal: await botik.SendTextMessageAsync(message.Chat.Id, "Это наш любимый Pascal?"); break;
					case FigureType.php: await botik.SendTextMessageAsync(message.Chat.Id, "Фи, это PHP?"); break;
					case FigureType.Ruby: await botik.SendTextMessageAsync(message.Chat.Id, "Это Ruby?"); break;
					default: await botik.SendTextMessageAsync(message.Chat.Id, "Я такого не знаю!"); break;
				}

				formUpdater("Picture recognized!");
				return;
			}

		   
			if (message.Type == MessageType.Text)
			{
				formUpdater( user.Name+": "+ message.Text);
				var answer = AIMLbot.Talk(user.Id, message.Text);
				await botik.SendTextMessageAsync(long.Parse(user.Id), answer);
				return;
			}
			if (message == null || message.Type != MessageType.Text) return;
			
			await botik.SendTextMessageAsync(message.Chat.Id, "Bot reply : " + message.Text);
			formUpdater(message.Text);
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
