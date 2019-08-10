using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Memoizer;
using System.IO;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace Memoizer.Tests
{
    [TestClass]
    public class MemoizerTests
    {
        private Memoizer memoizer;
        private TestClass testObject;
        private ICache cache;

        [TestInitialize]
        public void Setup()
        {
            testObject = Substitute.For<TestClass>();
            cache = Substitute.For<ICache>();
            memoizer = new Memoizer(cache);
        }

        [TestMethod]
        public void ItReturnsTheSameSignatureAsCalledWith()
        {
            string result = memoizer.Memoize(() => TestMethod(0, ""));
        }

        [TestMethod]
        public void ItPassesThroughResultAndParameters()
        {
            testObject.TestMethod(1, "A").Returns("mockResult");
            string result = memoizer.Memoize(() => testObject.TestMethod(1, "A"));
            Assert.AreEqual("mockResult", result);
            testObject.Received().TestMethod(1, "A");
        }

        [TestMethod]
        public void ItPassesParametersAndResultToCache()
        {
            testObject.TestMethod(2, "B").Returns("mr");
            string result = memoizer.Memoize(() => testObject.TestMethod(2, "B"));
            cache.Received().Store(Arg.Is<IEnumerable<object>>(ar => ar.Count() == 4 && (int)ar.Skip(2).First() == 2 && (string)ar.Skip(3).First() == "B"), "mr");
        }

        [TestMethod]
        public void ItReturnsValueFromCache()
        {
            testObject.TestMethod(3, "C").Returns("mr2");
            cache.Get(null).ReturnsForAnyArgs("cachedResult");
            string result = memoizer.Memoize(() => testObject.TestMethod(3, "C"));
            Assert.AreEqual("cachedResult", result);
            testObject.DidNotReceive().TestMethod(3, "C");
        }

        [TestMethod]
        public void ItDoesntCallParameterMethodASecondTime()
        {
            string result = memoizer.Memoize(() => testObject.TestMethod(3, testObject.TestMethod2()));
            testObject.Received(1).TestMethod2();
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void ItComplainsIfNonMethodCallPassed()
        {
            int result = memoizer.Memoize(() => 21);
        }

        [TestMethod]
        public void ItPassesTheFullNameOfTheClassAndMethodToCache()
        {
            string result = memoizer.Memoize(() => new TestClass().TestMethod(3, "N"));
            cache.Received().Store(Arg.Is<IEnumerable<object>>(ar => ar.Count() == 4 && (string)ar.First() == typeof(TestClass).AssemblyQualifiedName
                && (string)ar.Skip(1).First() == "System.String TestMethod(Int32, System.String)" && (int)ar.Skip(2).First() == 3 && (string)ar.Skip(3).First() == "N"), "nothing");
        }

        [TestMethod]
        public void ItPassesTheNameOfGenericMethods()
        {
            string result = memoizer.Memoize(() => new TestClass().TestMethod3<string>("N"));
            cache.Received().Store(Arg.Is<IEnumerable<object>>(ar => (string)ar.Skip(1).First() == "System.String TestMethod3[String](System.String)"), "something else");
        }

        [TestMethod]
        public void ItPassesTheNameAndClassOfStaticMethods()
        {
            string result = memoizer.Memoize(() => TestClass.TestMethod4(6));
            cache.Received().Store(Arg.Is<IEnumerable<object>>(ar => (string)ar.First() == typeof(TestClass).AssemblyQualifiedName &&
                (string)ar.Skip(1).First() == "System.String TestMethod4(Int32)"), "another thing");
        }

        private string TestMethod(int i, string f)
        {
            return "result";
        }

        public class TestClass
        {
            public virtual string TestMethod(int i, string f)
            {
                return "nothing";
            }

            public virtual string TestMethod2()
            {
                return "something";
            }

            public virtual string TestMethod3<T>(string p)
            {
                return "something else";
            }

            public static string TestMethod4(int p)
            {
                return "another thing";
            }
        }
    }
}