namespace Sandaab.Core
{
    public interface IApp
    {
        public void Invoke(EventHandler eventHandler, object sender, EventArgs args);
        
        public void InvokeAsync(EventHandler eventHandler, object sender, EventArgs args);

        public void RunOnUiThread(Action action);

        public void RunOnUiThreadAsync(Action action);
    }
}
