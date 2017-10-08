namespace AssemblyToProcess
{
    using System.ComponentModel.Composition;

    using NUnit.Framework;

    internal class BareObject
    {
        [Export]
        public static void SameObjectIsEqual()
        {
            var left = new BareObject();
            var right = left;

            Assert.AreEqual(left, right);
        }

        [Export]
        public static void DifferentObjectsAreNotEqual()
        {
            var left = new BareObject();
            var right = new BareObject();

            Assert.AreNotEqual(left, right);
        }

        // [Export]
        public static void ForceFail()
        {
            Assert.Inconclusive("Forced error.");
        }
    }
}
