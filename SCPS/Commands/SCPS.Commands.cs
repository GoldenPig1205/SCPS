﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace SCPS.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class Help : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            bool result;

            response = "\n.레벨 (AI1 Level) (AI2 Level) (AI3 Level) .. : \n나열된 SCP들의 레벨(0~20, 0은 비활성화)을 설정합니다. 모두 기입하여야 합니다.\n" +
                       ".공략 (SCP 이름 ex. SCP-106) : 각 SCP를 공략하는 방법을 출력합니다.";
            result = true;

            return result;
        }

        public string Command { get; } = "도움말";

        public string[] Aliases { get; } = { "도움" };

        public string Description { get; } = "도움말을 확인합니다.";

        public bool SanitizeResponse { get; } = true;
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class Method : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                bool result;

                response = $"\n{string.Join("\n\n", SCPS.Instance.Method.Select(pair => $"[{pair.Key}]\n {pair.Value}"))}\n\n- 모든 SCP는 CCTV로 쳐다보면 둔해집니다.\n- CCTV를 보려면 사무실 정면 우측 하단에 있는 워크스테이션과 상호작용 하십시오.\r\n- CCTV를 그만 보려면 핑(E 키)을 찍으십시오.\r\n- CCTV로 SCP들을 보고 있으면 평소보다 둔해집니다. (SCP-049-2 제외, SCP-106의 경우 효과를 3배 받습니다.)\r\n- SCP의 비명 소리는 각기 다릅니다. 소리 크기에 주의하십시오.\r\n- SCP의 첫 시작 장소는 SCP:SL의 SCP 스폰 장소와 동일합니다.";
                result = true;

                return result;
            }
            catch (Exception ex)
            {
                bool result;

                response = $"올바르지 않은 양식입니다!";
                result = false;

                return result;
            }
        }

        public string Command { get; } = "공략";

        public string[] Aliases { get; } = { };

        public string Description { get; } = ".공략 (SCP 이름 ex. SCP-106)";

        public bool SanitizeResponse { get; } = true;
    }

    [CommandHandler(typeof(ClientCommandHandler))]
    public class SetLevel : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            bool result;
            Player player = Player.Get(sender as CommandSender);

            try
            {
                if (!Round.IsStarted)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        int level = 0;

                        if (int.Parse(arguments.At(i)) > 20)
                            level = 20;

                        else
                            level = int.Parse(arguments.At(i));

                        SCPS.Instance.SetLevel[SCPS.Instance.SetLevel.Keys.ElementAt(i)] = level;
                    }


                    response = "성공적으로 AI 레벨을 설정하였습니다!";
                    result = true;

                    return result;
                }
                else
                {
                    response = "게임이 이미 시작되었습니다!";
                    result = false;

                    return result;
                }
            }
            catch (Exception ex)
			{
                response = "올바르지 않은 양식입니다! 모든 SCP의 레벨을 설정해야 합니다!";
                result = true;

                return result;
            }
            finally
            {
                SCPS.Instance.IsSetLevel = true;
            }
            
        }

        public string Command { get; } = "레벨";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "레벨 (AI1 Level) (AI2 Level) (AI3 Level) ..";

        public bool SanitizeResponse { get; } = true;
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class Rotate : ICommand
	{
		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			bool result;

			Gtool.Rotate(SCPS.Instance.Chracters.Find(x => x.Name == arguments.At(0)).npc, new Vector3(float.Parse(arguments.At(1)), float.Parse(arguments.At(2)), float.Parse(arguments.At(3))));
			response = "Success!";
			result = true;

			return result;
		}

		public string Command { get; } = "rotate";

		public string[] Aliases { get; } = { "rot", "rt" };

		public string Description { get; } = "rot <target> <x> <y> <z>";

		public bool SanitizeResponse { get; } = true;
	}

	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class Battery : ICommand
	{
		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			bool result;

			SCPS.Instance.Battery = int.Parse(arguments.At(0));
			response = "Success!";
			result = true;

			return result;
		}

		public string Command { get; } = "battery";

		public string[] Aliases { get; } = { "bty" };

		public string Description { get; } = "bty <ammount>";

		public bool SanitizeResponse { get; } = true;
	}
}
