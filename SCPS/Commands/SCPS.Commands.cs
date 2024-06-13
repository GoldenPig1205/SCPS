using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;
using UnityEngine;

namespace SCPS.Commands
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class Rotate : ICommand
	{
		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
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
		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
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
