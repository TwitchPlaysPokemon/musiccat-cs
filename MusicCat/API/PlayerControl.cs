using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ApiListener;
using MusicCat.Players;

namespace MusicCat.API
{
    public class PlayerControl : ApiProvider
    {
        public override IEnumerable<ApiCommand> Commands => new List<ApiCommand>() {
            new ApiCommand("Play", args=>EachPlayerCommand(p => p.Play()), new List<ApiParameter>(), "Tells the music player(s) to play the current track."),
            new ApiCommand("Pause", args=>EachPlayerCommand(p => p.Pause()), new List<ApiParameter>(), "Tells the music player(s) to pause the current track."),
            new ApiCommand("Stop", args=>EachPlayerCommand(p => p.Stop()), new List<ApiParameter>(), "Tells the music player(s) to stop playing."),
            new ApiCommand("PlayFile", args=>EachPlayerCommand(p => p.PlayFile(args.Count() > 0 ? string.Join("\\",args) : throw new ApiError("Filename was not specified"))), new List<ApiParameter>{ new ApiParameter("Filename","string") }, "Tells the music player(s) to play a specific file."),
            new ApiCommand("SetVolume", args=>EachPlayerCommand(p => p.SetVolume(ParseRequired(args, 0, s=>float.Parse(s), "Level"))), new List<ApiParameter>{ new ApiParameter("Level","float") }, "Sets the volume of the music player(s) to the specified level (between 0 and 1)."),
            new ApiCommand("SetPosition", args=>EachPlayerCommand(p => p.SetPosition(ParseRequired(args, 0, s=>float.Parse(s), "Position"))), new List<ApiParameter>{ new ApiParameter("Position","float") }, "Sets the playhead of the music player(s) to the specified position (between 0 and 1)."),
        };

        public PlayerControl()
        {
            if (Listener.Config.AjaxAMPConfig != null)
            {
                Players.Add(new AjaxAMP(Listener.Config.AjaxAMPConfig));
            }
            //Add other players here, control which are loaded by setting the appropriate config
        }

        private List<IPlayer> Players = new List<IPlayer>();

        private void EachPlayerCommand(Func<IPlayer, Task> action) => Task.WaitAll(Players.Select(p => action(p)).ToArray());

    }
}
