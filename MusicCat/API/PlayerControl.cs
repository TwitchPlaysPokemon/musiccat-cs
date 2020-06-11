using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ApiListener;
using MusicCat.Players;
using static ApiListener.ApiLogger;

namespace MusicCat.API
{
	public class PlayerControl : ApiProvider
    {
        public override IEnumerable<ApiCommand> Commands => new List<ApiCommand> {
			new ApiCommand("Launch", args=>EachPlayerCommand(p => p.Launch()), new List<ApiParameter>(), "Launches Winamp."),
            new ApiCommand("Play", args=>EachPlayerCommand(p => p.Play()), new List<ApiParameter>(), "Tells the music player(s) to play the current track."),
            new ApiCommand("Pause", args=>EachPlayerCommand(p => p.Pause()), new List<ApiParameter>(), "Tells the music player(s) to pause the current track."),
            new ApiCommand("Stop", args=>EachPlayerCommand(p => p.Stop()), new List<ApiParameter>(), "Tells the music player(s) to stop playing."),
            new ApiCommand("PlayFile", args=>EachPlayerCommand(p => p.PlayFile(args.Any() ? string.Join("\\",args) : throw new ApiError("Filename was not specified"))), new List<ApiParameter>{ new ApiParameter("Filename","string") }, "Tells the music player(s) to play a specific file."),
			new ApiCommand("PlayId", args=>EachPlayerCommand(p => p.PlayID(args.Any() ? string.Join(" ", args) : throw new ApiError("ID was not specified"))), new List<ApiParameter> {new ApiParameter("Id", "string")}, "Tells the music player to play the song with the specific id"),
			new ApiCommand("SetVolume", args=>EachPlayerCommand(p => p.SetVolume(ParseRequired(args, 0, s=>float.Parse(s), "Level"))), new List<ApiParameter>{ new ApiParameter("Level","float") }, "Sets the volume of the music player(s) to the specified level (between 0 and 1)."),
            new ApiCommand("GetVolume", args=>EachPlayerQuery(p=>p.GetVolume()), new List<ApiParameter>(), "Gets the volume level (between 0 and 1) of the first music player. Returns a float"),
            new ApiCommand("SetPosition", args=>EachPlayerCommand(p => p.SetPosition(ParseRequired(args, 0, s=>float.Parse(s), "Position"))), new List<ApiParameter>{ new ApiParameter("Position","float") }, "Sets the playhead of the music player(s) to the specified position (between 0 and 1)."),
            new ApiCommand("GetPosition", args=>EachPlayerQuery(p=>p.GetPosition()), new List<ApiParameter>(), "Gets the playhead position (from 0 to 1) of the first music player. Returns a float")
        };

        public PlayerControl(Action<ApiLogMessage> logger) : base(logger)
        {
            if (Listener.Config.AjaxAMPConfig != null)
            {
                Players.Add(new AjaxAMP(Listener.Config.AjaxAMPConfig));
                Log(Info("Controlling Winamp via AjaxAMP"));
            }
            //Add other players here, control which are loaded by setting the appropriate config
        }

        private List<IPlayer> Players = new List<IPlayer>();

        private void EachPlayerCommand(Func<IPlayer, Task> action) => Task.WaitAll(Players.Select(p => action(p)).ToArray());
        private T EachPlayerQuery<T>(Func<IPlayer, Task<T>> action) => Task.WhenAll(Players.Select(p => action(p)).ToArray()).Result.FirstOrDefault(); //TODO: Make this support results from multiple players

    }
}
