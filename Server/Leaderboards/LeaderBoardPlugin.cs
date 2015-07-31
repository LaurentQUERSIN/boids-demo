﻿using Stormancer;
using Stormancer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Database;

namespace Server.Leaderboards
{
    class LeaderBoardPlugin : IHostPlugin
    {
        public LeaderBoardPlugin(IAppBuilder builder)
        {
            builder.AdminPlugin("leaderboard", Stormancer.Server.Admin.AdminPluginHostVersion.V0_1).Name("Leaderboard");
        }

        public void Build(HostPluginBuildContext ctx)
        {
            
            ctx.HostStarting += h =>
            {
                h.DependencyResolver.Register<ILeaderboardsService, LeaderboardsService>();
            };
        }
    }

    public class Startup
    {
        public void Run(IAppBuilder builder)
        {
            builder.AddPlugin(new LeaderBoardPlugin(builder));
            
        }
    }
}
