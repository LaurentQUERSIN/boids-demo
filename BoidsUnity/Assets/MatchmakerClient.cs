using System;
using System.Threading.Tasks;
using Stormancer;
using System.Linq;

public class FindMatchResult
{
    public string Token { get; set; }

}

public class MatchmakerClient
{
    private readonly Scene _scene;
    public MatchmakerClient(Scene matchmakerScene)
    {
        _scene = matchmakerScene;
    }

    public Task<Scene> Connect()
    {
        return _scene.Connect().Then(() => _scene);
    }

    public  Task<FindMatchResult> FindMatch()
    {
        return _scene.RpcTask<bool, FindMatchResult>("match.find", true);
    }
}