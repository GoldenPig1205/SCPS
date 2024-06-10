using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;

namespace SCPS.Commands
{
	[CommandHandler(typeof(ClientCommandHandler))]
	public class Test : ICommand
	{
		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
		{
			bool result;

			try
			{
				if (arguments.At(0) != null)
				{
					response = $"테스트 내용 : {arguments.At(0)}";

					result = true;
				}
				else
				{
					response = $"안타깝네요.";

					result = true;
				}
			}
			catch (Exception ex)
			{
				response = $"오류났습니다. ㅅㄱ \n{ex}";

				result = false;
			}

			return result;
		}

		public string Command { get; } = "test";

		public string[] Aliases { get; } = Array.Empty<string>();

		public string Description { get; } = "돼지가 테스트용으로 만든 명령어";

		public bool SanitizeResponse { get; } = true;
	}
}
