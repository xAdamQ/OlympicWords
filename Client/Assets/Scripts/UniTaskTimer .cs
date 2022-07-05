// why I dumped it:
// i can't stop the unitask straight forward as i do
//     with coroutines
//
// using Cysharp.Threading.Tasks;
// using System;
// using System.Threading;
//
// public class UniTaskTimer
// {
//     //in milliseconds
//     private readonly int _timeStep;
//     private readonly int _interval;
//
//     public event Action Elapsed;
//     public event Action<float> Ticked;
//
//     public bool IsPlaying { get; private set; }
//     public bool IsCancelled { get; private set; }
//
//     public UniTaskTimer(int interval, int timeStep, Action elapsed = null, Action<float> ticked = null)
//     {
//         _interval = interval;
//         _timeStep = timeStep;
//         Elapsed += elapsed;
//         Ticked += ticked;
//     }
//
//
//     public void Play()
//     {
//         if (IsPlaying)
//         {
//             IsCancelled = true;
//             //await the timestep
//         }
//
//         Play(LastPlayCTS.Token).Forget();
//     }
//
//     private async UniTask Play(CancellationToken ct)
//     {
//         IsPlaying = true;
//
//         var ticksCount = _interval / _timeStep;
//         for (var i = 0; i < ticksCount; i++)
//         {
//             var progress = (float) i / ticksCount;
//             Ticked?.Invoke(progress);
//
//             await UniTask.Delay(_timeStep);
//
//             if (ct.IsCancellationRequested)
//                 break;
//         }
//
//         if (IsPlaying) Elapsed?.Invoke(); //did it finish normally or terminated
//
//         IsPlaying = false;
//         //when cancelled by another timer is not as stopping
//     }
//
//     public void Stop()
//     {
//         IsPlaying = false;
//     }
// }

