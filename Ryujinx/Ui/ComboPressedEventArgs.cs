using System;

namespace Ryujinx.Ui
{
    public class ComboPressedEventArgs : EventArgs
    {
        public int Combo;

        public ComboPressedEventArgs(int combo)
        {
            Combo = combo;
        }
    }
}
