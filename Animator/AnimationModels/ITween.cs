using System;
using System.Collections.Generic;

namespace AnimationModels
{
    public interface ITween
    {
        List<KeyFrame> GenerateInbetweenFrames(KeyFrame fristKeyFrame, KeyFrame secondKeyFrame);

        int GeneratedFrameCount
        {
            get;
            set;
        }
    }
}
