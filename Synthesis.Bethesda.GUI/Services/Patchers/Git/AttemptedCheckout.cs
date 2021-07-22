namespace Synthesis.Bethesda.GUI.Services.Patchers.Git
{
    public interface IAttemptedCheckout
    {
        bool Attempted(CheckoutInput input);
    }

    public class AttemptedCheckout : IAttemptedCheckout
    {
        public bool Attempted(CheckoutInput input)
        {
            return input.RunnerState.RunnableState.Succeeded
                   && input.Proj.Succeeded
                   && input.LibraryNugets.Succeeded;
        }
    }
}