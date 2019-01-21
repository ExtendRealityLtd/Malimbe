namespace Malimbe.BehaviourStateRequirementMethod
{
    using System;

    /// <summary>
    /// Indicates that the method returns early in case a specific GameObject state or Behaviour state isn't matched.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequiresBehaviourStateAttribute : Attribute
    {
        // ReSharper disable MemberCanBePrivate.Global
        /// <summary>
        /// The required active state of the GameObject that the component the method is on is added to.
        /// </summary>
        public readonly GameObjectActivity GameObjectActivity;
        /// <summary>
        /// The required state of the Behaviour.
        /// </summary>
        public readonly bool BehaviourNeedsToBeEnabled;
        // ReSharper restore MemberCanBePrivate.Global

        /// <summary>
        /// Indicates that the method returns early in case a specific GameObject state or Behaviour state isn't matched.
        /// </summary>
        /// <param name="gameObjectActivity">The required active state of the GameObject that the component the method is on is added to.</param>
        /// <param name="behaviourNeedsToBeEnabled">The required state of the Behaviour.</param>
        public RequiresBehaviourStateAttribute(
            GameObjectActivity gameObjectActivity = GameObjectActivity.InHierarchy,
            bool behaviourNeedsToBeEnabled = true)
        {
            GameObjectActivity = gameObjectActivity;
            BehaviourNeedsToBeEnabled = behaviourNeedsToBeEnabled;
        }
    }
}
