namespace RevitMate.Core.Conversation
{
    /// <summary>
    /// Tracks, for a single user turn, whether the user has approved a plan that
    /// authorizes model-mutating tools to run. A fresh instance (or a
    /// <see cref="Reset"/>) is used at the start of every turn so approval never
    /// leaks across turns.
    /// </summary>
    public sealed class PlanSession
    {
        /// <summary>True once the user has approved a proposed plan this turn.</summary>
        public bool IsApproved { get; private set; }

        /// <summary>Records the user's approval of the current turn's plan.</summary>
        public void Approve() => IsApproved = true;

        /// <summary>Clears approval at the start of a new turn.</summary>
        public void Reset() => IsApproved = false;

        /// <summary>
        /// Whether a tool may run now: read-only tools always may; a mutating tool
        /// may only after the user has approved a plan this turn.
        /// </summary>
        public bool MayExecute(bool isMutating) => !isMutating || IsApproved;
    }
}
