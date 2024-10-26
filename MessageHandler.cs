﻿using Microsoft.Extensions.Configuration;
using System.Data;
using Telegram.Bot.Types.ReplyMarkups;
using static DestarionBot.BotClient;
using static DestarionBot.Configuration;
namespace DestarionBot
{
    internal static class MessageHandler
    {
        public enum CommandType
        {
            Start,
            AddCharacter,
            DeleteCharacter,
            ChooseServer,
            CurrentServer,
            ChooseLanguage,
            Help,
            Unknown
        }
        public static async Task Handle(User user, string message)
        {
            CommandType commandType = GetCommandType(message);
            if (user.State == "/add_character" || user.State == "/delete_character")
            {
                if(!String.IsNullOrEmpty(user.Server))
                {
                    if (message.Length == 9 && int.TryParse(message, out int id))
                    {
                        switch (user.State)
                        {
                            case "/add_character":
                                await user.ProcessRegistering(id);
                                return;
                            case "/delete_character":
                                await user.ProcessDeleting(id);
                                return;
                        }
                    }
                }
                else
                {
                    await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.ServerSelectionPrompt), await BuildKeyboard(user, CommandType.ChooseServer));
                    return;
                }
            }
            switch(commandType)
            {
                case CommandType.Start:
                    await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.Start), await BuildKeyboard(user, CommandType.ChooseServer));
                    break;
                case CommandType.AddCharacter:
                    user.State = message;
                    if (!String.IsNullOrEmpty(user.Server))
                        await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.SendIdPrompt));
                    else 
                        await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.ServerSelectionPrompt), await BuildKeyboard(user, CommandType.ChooseServer));
                    break;
                case CommandType.DeleteCharacter:
                    user.State = message;
                    if (!String.IsNullOrEmpty(user.Server))
                        await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.SendIdPrompt));
                    else
                        await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.ServerSelectionPrompt), await BuildKeyboard(user, CommandType.ChooseServer));
                    break;
                case CommandType.CurrentServer:
                    await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.CurrentServerInfo, new string[] { String.IsNullOrEmpty(user.Server) ? "null" : user.Server}));
                    break;
                case CommandType.ChooseLanguage:
                    await Bot.SendTextMessageAsync(user.ChatId, "Choose language", await BuildKeyboard(user, CommandType.ChooseLanguage));
                    break;
                case CommandType.ChooseServer:
                    await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.ServerSelectionPrompt), await BuildKeyboard(user, CommandType.ChooseServer));
                    break;
                case CommandType.Help:
                    await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.Help));
                    break;
                case CommandType.Unknown:
                    await Bot.SendTextMessageAsync(user.ChatId, await Build(user, Language.MessageType.UnknownCommand));
                    break;
            }
        }
        public static async Task<string> Build(User user, Language.MessageType type, string[] args = null)
        {
            var response = await Language.Get(user.Language, type);
            if(args is not null && args.Length > 0)
            {
                 response = await ReplaceHolders(response, args);
            }
            return response;
        }
        private static async Task<string> ReplaceHolders(string text, string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                text = text.Replace("{" + i + "}", args[i]);
            return text;
        }
        public static async Task<InlineKeyboardMarkup> BuildKeyboard(User user, CommandType type)
        {
            InlineKeyboardButton[] buttons;
            InlineKeyboardMarkup keyboard;
            switch (type)
            {
                case CommandType.ChooseServer:
                    buttons = BotService.servers.Select(server => InlineKeyboardButton.WithCallbackData(server)).ToArray();
                    keyboard = new InlineKeyboardMarkup(buttons);
                    break;
                case CommandType.ChooseLanguage:
                    var languageButtons = Language.Languages
                        .Select(lang => InlineKeyboardButton.WithCallbackData(lang, lang))
                        .ToList();

                    var rows = new List<InlineKeyboardButton[]>();

                    for (int i = 0; i < languageButtons.Count; i += 3)
                    {
                        rows.Add(languageButtons.Skip(i).Take(3).ToArray());
                    }

                    keyboard = new InlineKeyboardMarkup(rows); 
                    break;
                default:
                    keyboard = null;
                    throw new NotImplementedException("Not implemented case in MessageHandler.BuildKeyboard!");
                    break;
            }
            return keyboard;
        }
        private static CommandType GetCommandType(string message)
        {
            return message.ToLower() switch
            {
                "/start" => CommandType.Start,
                "/add_character" => CommandType.AddCharacter,
                "/delete_character" => CommandType.DeleteCharacter,
                "/choose_server" => CommandType.ChooseServer,
                "/current_server" => CommandType.CurrentServer,
                "/choose_language" => CommandType.ChooseLanguage,
                "/help" => CommandType.Help,
                _ => CommandType.Unknown
            };
        }
    }
}
