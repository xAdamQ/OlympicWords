using UnityEngine;

/// <summary>
/// this controller is used to derive the functionality from
/// </summary>
public abstract class GraphController<TPlayer> : PlayerController<TPlayer> where TPlayer : GraphPlayer
{
    protected override void Awake()
    {
        base.Awake();

        //word state is now for any controller, but it should be for graph controllers
        Player.PowerSkipping += w => GraphEnv.I.WordState(w, false);
        Player.PowerSkipped += lastWordIndex =>
        {
            GraphEnv.I.WordState(Player.WordIndex, true);
            if (Player.WordIndex + 1 < GraphEnv.I.WordsCount)
                GraphEnv.I.WordState(Player.WordIndex + 1, true);

            for (var i = lastWordIndex + 1; i <= Player.WordIndex - 1; i++)
                GraphEnv.I.WordState(i, false);
        };

        Player.GoingToNextWord += () => GraphEnv.I.WordState(Player.WordIndex, false);
        Player.GoneToNextWord += () =>
        {
            //current word + 1
            if (Player.WordIndex < GraphEnv.I.WordsCount - 1)
                GraphEnv.I.WordState(Player.WordIndex + 1, true);
        };
    }

    protected override void Start()
    {
        base.Start();

        GraphEnv.I.WordState(0, true);
        GraphEnv.I.WordState(1, true);
    }
}

/// <summary>
/// this is a usable controller
/// </summary>
public class GraphController : GraphController<GraphPlayer>
{
    protected override Transform CameraTarget => transform;
}