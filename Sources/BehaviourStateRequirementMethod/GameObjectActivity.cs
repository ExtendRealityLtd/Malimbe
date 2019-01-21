namespace Malimbe.BehaviourStateRequirementMethod
{
    /// <summary>
    /// The active state of a GameObject.
    /// </summary>
    public enum GameObjectActivity
    {
        /// <summary>
        /// The GameObject active state is of no interest.
        /// </summary>
        None = 0,
        /// <summary>
        /// The GameObject itself needs to be active, the state of parent GameObjects is ignored.
        /// </summary>
        Self,
        /// <summary>
        /// The GameObject is active in the scene because it is active itself and all parent GameObjects are, too.
        /// </summary>
        InHierarchy
    }
}
