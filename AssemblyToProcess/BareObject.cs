namespace AssemblyToProcess
{
    using System.ComponentModel.Composition;

    using Xunit;

    internal class BareObject
    {
        [Export]
        public static void SameObjectIsEqual()
        {
            var left = new BareObject();
            var right = left;

            Assert.Equal(left, right);
        }

        [Export]
        public static void DifferentObjectsAreNotEqual()
        {
            var left = new BareObject();
            var right = new BareObject();

            Assert.NotEqual(left, right);
        }

        // [Export]
        public static void ForceFail()
        {
            Assert.False(true);
        }
    }
}
