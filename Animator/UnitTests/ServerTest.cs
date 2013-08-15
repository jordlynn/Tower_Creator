using AnimatorServer;
using AnimatorClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Sockets;
using AnimationModels;
using System.Collections.Generic;

namespace UnitTests
{
    /// <summary>
    ///This is a test class for ServerTest and is intended
    ///to contain all ServerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ServerTest
    {
        [TestMethod]
        public void TestAnimationUpload()
        {
            AnimationServer server = new AnimationServer();
            server.Port = 1334;

            AnimatorClient.AnimatorClient client = new AnimatorClient.AnimatorClient();

            client.ServerAddress = "localhost";
            client.ServerPort = 1334;

            server.IsListening = true;

            Animation testAnimation = new Animation(10, 10);
            testAnimation.Name = "Test Animation";

            client.UploadAnimation(testAnimation);

            Assert.AreEqual(1, server.Animations.Count);
            Assert.AreEqual("Test Animation", server.Animations[0].Name);

            server.IsListening = false;
        }

        [TestMethod]
        public void TestListAnimations()
        {
            AnimationServer server = new AnimationServer();
            server.Port = 1334;

            AnimatorClient.AnimatorClient client = new AnimatorClient.AnimatorClient();

            client.ServerAddress = "localhost";
            client.ServerPort = 1334;

            server.IsListening = true;

            var titles = client.GetAnimationTitles();

            Assert.AreEqual(0, titles.Count);

            Animation testAnimation = new Animation(10, 10);
            testAnimation.Name = "Test Animation";

            client.UploadAnimation(testAnimation);

            titles = client.GetAnimationTitles();
            Assert.AreEqual(1, titles.Count);
            Assert.AreEqual("Test Animation", titles[0]);

            server.IsListening = false;
        }

        [TestMethod]
        public void TestDeleteAnimation()
        {
            AnimationServer server = new AnimationServer();
            server.Port = 1334;

            AnimatorClient.AnimatorClient client = new AnimatorClient.AnimatorClient();

            client.ServerAddress = "localhost";
            client.ServerPort = 1334;

            server.IsListening = true;

            Animation testAnimation = new Animation(10, 10);
            testAnimation.Name = "Test Animation 1";

            client.UploadAnimation(testAnimation);
            testAnimation.Name = "Test Animation 2";
            client.UploadAnimation(testAnimation);

            // while (server.Status != ServerStatus.Listening) ;

            Assert.AreEqual(2, server.Animations.Count);

            client.DeleteAnimation(0);

            Assert.AreEqual(1, server.Animations.Count);
            Assert.AreEqual("Test Animation 2", server.Animations[0].Name);
            
            server.IsListening = false;
        }
    }
}
