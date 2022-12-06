using UnityEditor;
using UnityEngine;
using NUnit.Framework;

namespace GeospatialPlaces.Tests
{
    public class FloorFinderTest
    {
        private FloorFinder floorFinder;

        [SetUp]
        public void SetUp()
        {
            floorFinder = new FloorFinder();
        }

        [TestCase("なんとか2 ビル1", null)]
        [TestCase("2FF", null)]
        [TestCase("2F", 2)]
        [TestCase("2階", 2)]
        [TestCase("ビル2F", 2)]
        [TestCase("ビル２階", 2)]
        [TestCase("ビル ２階", 2)]
        [TestCase("ビル　２階", 2)]
        [TestCase("１５階", 15)]
        [TestCase("ビル　２階　奥", 2)]
        [TestCase("ビル　２階奥", null)]
        [TestCase("B2F", null)]
        [TestCase("地下2F", null)]
        public void Test(string address, int? expected)
        {
            var result = floorFinder.FindFloor(address);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}