using System.Runtime.InteropServices;

namespace ChessDroid.Controls
{
    /// <summary>
    /// CHESSDROID_AUDIT.md §4.2: "Win32 combo hover preview appears 3 times" — the
    /// GetComboBoxInfo+SendMessage(LB_GETCURSEL) block used to find which dropdown item the
    /// mouse is currently over (WinForms raises no such event) was copy-pasted into 3
    /// separate timer-tick handlers in AnalysisBoardForm (piece set, board color, drill
    /// chapter), each doing its own distinct preview action afterward. Extracted here —
    /// the preview *action* per combo still lives in its own handler, only the native
    /// hovered-index lookup itself is shared. (A full HoverPreviewComboBox custom-control
    /// subclass, as the audit sketches, would also need the Designer-declared field types
    /// changed on all 3 combos — not attempted here, since that can't be verified without
    /// the WinForms designer open and regenerating code, only reasoned about from source.)
    /// </summary>
    internal static class ComboBoxHoverHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct COMBOBOXINFO
        {
            public int cbSize;
            public int rcItemLeft, rcItemTop, rcItemRight, rcItemBottom;
            public int rcButtonLeft, rcButtonTop, rcButtonRight, rcButtonBottom;
            public int stateButton;
            public IntPtr hwndCombo, hwndItem, hwndList;
        }

        [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(IntPtr hwnd, ref COMBOBOXINFO pcbi);
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private const int LB_GETCURSEL = 0x0188;

        /// <summary>
        /// Returns the index of the dropdown item currently under the mouse, or null if the
        /// combo's native listbox isn't available or nothing is hovered (same early-return
        /// conditions every original call site used — just no longer duplicated).
        /// </summary>
        internal static int? GetHoveredIndex(ComboBox combo)
        {
            var info = new COMBOBOXINFO { cbSize = Marshal.SizeOf<COMBOBOXINFO>() };
            if (!GetComboBoxInfo(combo.Handle, ref info) || info.hwndList == IntPtr.Zero)
                return null;

            int idx = SendMessage(info.hwndList, LB_GETCURSEL, 0, 0);
            if (idx < 0 || idx >= combo.Items.Count)
                return null;

            return idx;
        }
    }
}
