using System;
namespace AnimatorServer
{
    interface IAnimator
    {
        void Pause();
        void Start();
        void Stop();
    }
}
