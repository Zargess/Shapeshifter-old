namespace Shapeshifter.Desktop.Functionality.Keyboard
{
    /// <summary>
    /// Raw keyevent handler.
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="args">Raw KeyEvent arguments</param>
    /// <param name="handled"></param>
    public delegate void RawKeyEventHandler(object sender, RawKeyEventArgs args, ref bool handled);
}