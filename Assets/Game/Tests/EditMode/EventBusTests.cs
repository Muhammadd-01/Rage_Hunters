#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using RageHunters.Core;

namespace RageHunters.Tests.EditMode
{
    public class EventBusTests
    {
        private struct TestSampleEvent
        {
            public int Value;
            public string Message;

            public TestSampleEvent(int value, string message)
            {
                Value = value;
                Message = message;
            }
        }

        [Test]
        public void EventBus_SubscribeAndPublish_DeliversPayload()
        {
            int receivedValue = 0;
            string receivedMessage = null;

            void OnEvent(TestSampleEvent evt)
            {
                receivedValue = evt.Value;
                receivedMessage = evt.Message;
            }

            EventBus.Subscribe<TestSampleEvent>(OnEvent);
            EventBus.Publish(new TestSampleEvent(42, "Hunter Spotted"));
            EventBus.Unsubscribe<TestSampleEvent>(OnEvent);

            Assert.AreEqual(42, receivedValue, "Subscriber should receive matching integer payload.");
            Assert.AreEqual("Hunter Spotted", receivedMessage, "Subscriber should receive matching message payload.");
        }

        [Test]
        public void EventBus_Unsubscribe_StopsDelivery()
        {
            int callCount = 0;

            void OnEvent(TestSampleEvent evt)
            {
                callCount++;
            }

            EventBus.Subscribe<TestSampleEvent>(OnEvent);
            EventBus.Publish(new TestSampleEvent(1, "First"));
            EventBus.Unsubscribe<TestSampleEvent>(OnEvent);
            EventBus.Publish(new TestSampleEvent(2, "Second"));

            Assert.AreEqual(1, callCount, "After unsubscribe, event callback should not be triggered.");
        }

        [Test]
        public void EventBus_MultipleSubscribers_AllReceiveEvent()
        {
            int sub1Count = 0;
            int sub2Count = 0;

            void Sub1(TestSampleEvent evt) => sub1Count++;
            void Sub2(TestSampleEvent evt) => sub2Count++;

            EventBus.Subscribe<TestSampleEvent>(Sub1);
            EventBus.Subscribe<TestSampleEvent>(Sub2);

            EventBus.Publish(new TestSampleEvent(99, "Broadcast"));

            EventBus.Unsubscribe<TestSampleEvent>(Sub1);
            EventBus.Unsubscribe<TestSampleEvent>(Sub2);

            Assert.AreEqual(1, sub1Count, "Subscriber 1 should receive broadcast.");
            Assert.AreEqual(1, sub2Count, "Subscriber 2 should receive broadcast.");
        }
    }
}
#endif
