namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IAttemptedCheckout
    {
        bool Attempted(PotentialCheckoutInput input);
    }

    public class AttemptedCheckout : IAttemptedCheckout
    {
        public bool Attempted(PotentialCheckoutInput input)
        {
            return input.RunnerState.RunnableState.Succeeded
                   && input.Proj.Succeeded
                   && input.LibraryNugets.Succeeded;
        }
    }
}