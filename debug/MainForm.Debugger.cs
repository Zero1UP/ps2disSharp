using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;

namespace PS2Disassembler
{
    public sealed partial class MainForm : Form
    {
        private Panel CreateBreakpointsSidebar()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
                Margin = Padding.Empty,
                Tag = "BreakpointsSidebar",
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _btnContinueEmu = new Button { Text = "Continue", Dock = DockStyle.Fill, Height = 30, Margin = new Padding(0, 0, 0, 6), MinimumSize = new Size(0, 30) };
            _btnContinueEmu.Click += (_, _) => ContinueExecution();

            var stepLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, AutoSize = false, Height = 28, Margin = new Padding(0, 0, 0, 6), GrowStyle = TableLayoutPanelGrowStyle.FixedSize };
            stepLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            stepLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            stepLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _btnStep = new Button { Text = "Step", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 3, 0), Height = 28, MinimumSize = new Size(0, 28) };
            _btnStepOver = new Button { Text = "Step Over", Dock = DockStyle.Fill, Margin = new Padding(3, 0, 0, 0), Height = 28, MinimumSize = new Size(0, 28) };
            _btnStep.Click += (_, _) => StepExecution(stepOver: false);
            _btnStepOver.Click += (_, _) => StepExecution(stepOver: true);
            stepLayout.Controls.Add(_btnStep, 0, 0);
            stepLayout.Controls.Add(_btnStepOver, 1, 0);

            var readPanel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 6), GrowStyle = TableLayoutPanelGrowStyle.FixedSize };
            readPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            readPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            readPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _chkReadBreakpoint = new ThemedCheckBox { Text = "OnRead:", AutoSize = true, Margin = new Padding(0, 4, 6, 0) };
            _txtReadBreakpoint = new CenteredSingleLineTextBox { Dock = DockStyle.Fill, MaxLength = 8, CharacterCasing = CharacterCasing.Upper, Margin = new Padding(0) };
            _chkReadBreakpoint.CheckedChanged += (_, _) => ApplyMemoryBreakpointInput(isRead: true, showErrors: false);
            _txtReadBreakpoint.Leave += (_, _) => ApplyMemoryBreakpointInput(isRead: true, showErrors: false);
            _txtReadBreakpoint.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplyMemoryBreakpointInput(isRead: true, showErrors: true);
                }
            };
            readPanel.Controls.Add(_chkReadBreakpoint, 0, 0);
            readPanel.Controls.Add(_txtReadBreakpoint, 1, 0);

            var writePanel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 6), GrowStyle = TableLayoutPanelGrowStyle.FixedSize };
            writePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            writePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            writePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _chkWriteBreakpoint = new ThemedCheckBox { Text = "OnWrite:", AutoSize = true, Margin = new Padding(0, 4, 6, 0) };
            _txtWriteBreakpoint = new CenteredSingleLineTextBox { Dock = DockStyle.Fill, MaxLength = 8, CharacterCasing = CharacterCasing.Upper, Margin = new Padding(0) };
            _chkWriteBreakpoint.CheckedChanged += (_, _) => ApplyMemoryBreakpointInput(isRead: false, showErrors: false);
            _txtWriteBreakpoint.Leave += (_, _) => ApplyMemoryBreakpointInput(isRead: false, showErrors: false);
            _txtWriteBreakpoint.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ApplyMemoryBreakpointInput(isRead: false, showErrors: true);
                }
            };
            writePanel.Controls.Add(_chkWriteBreakpoint, 0, 0);
            writePanel.Controls.Add(_txtWriteBreakpoint, 1, 0);

            var callStackLabel = new Label { Text = "Call stack", Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
            _txtCallStack = new ThemedCallStackTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                Margin = new Padding(0, 0, 0, 6),
                MinimumSize = new Size(0, 110),
            };
            _txtCallStack.MouseDoubleClick += OnCallStackMouseDoubleClick;

            _fprList = CreateRegisterListControl();

            layout.Controls.Add(_btnContinueEmu, 0, 0);
            layout.Controls.Add(stepLayout, 0, 1);
            layout.Controls.Add(readPanel, 0, 2);
            layout.Controls.Add(writePanel, 0, 3);
            layout.Controls.Add(_fprList, 0, 5);

            panel.Controls.Add(layout);

            var callStackHost = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty };
            callStackLabel.Dock = DockStyle.Top;
            _txtCallStack.Dock = DockStyle.Fill;
            callStackHost.Controls.Add(_txtCallStack);
            callStackHost.Controls.Add(callStackLabel);
            layout.Controls.Add(callStackHost, 0, 4);

            return panel;
        }

        private VirtualDisasmList CreateRegisterListControl()
        {
            var lv = new VirtualDisasmList
            {
                Dock = DockStyle.Fill,
                VirtualMode = true,
                OwnerDraw = true,
                FullRowSelect = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HeaderHeight = 20,
                RowHeight = 18,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Tag = "RegisterList",
            };
            lv.Columns.Add("Reg", 44);
            lv.Columns.Add("Value", 180);
            lv.RetrieveVirtualItem += OnRegisterListRetrieveVirtualItem;
            lv.DrawHeader += OnRegisterListDrawHeader;
            lv.DrawCell += OnRegisterListDrawCell;
            lv.Resize += (_, _) => UpdateRegisterListColumns(lv);
            lv.ColumnWidthChanged += (_, _) => UpdateRegisterListColumns(lv, preserveFirstColumn: true);
            lv.MouseDoubleClick += OnRegisterListDoubleClick;
            return lv;
        }

        private TextBox? _regInlineEdit;

        private void OnRegisterListDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_fprList == null) return;
            var hit = _fprList.HitTest(new Point(e.X, e.Y));
            if (hit?.Item == null) return;
            int rowIdx = hit.Item.Index;
            int col = hit.ColumnIndex;
            if (col != 1) return; // only Value column
            if (rowIdx < 0 || rowIdx >= _fprRows.Count) return;

            string value = _fprRows[rowIdx].Value;

            // Calculate cell bounds: column 0 width + header height offset
            int x = 0;
            for (int c = 0; c < col; c++)
                x += _fprList.Columns[c].Width;
            int y = _fprList.HeaderHeight + ((rowIdx - _fprList.TopIndex) * _fprList.RowHeight);
            int w = _fprList.Columns[col].Width;
            int h = _fprList.RowHeight;

            if (_regInlineEdit == null)
            {
                _regInlineEdit = new TextBox
                {
                    ReadOnly = true,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = this.Font,
                };
                _regInlineEdit.KeyDown += (_, ke) =>
                {
                    if (ke.KeyCode == Keys.Escape || ke.KeyCode == Keys.Enter)
                    {
                        _regInlineEdit.Visible = false;
                        _fprList.Focus();
                    }
                };
                _regInlineEdit.Leave += (_, _) =>
                {
                    if (_regInlineEdit != null) _regInlineEdit.Visible = false;
                };
                _fprList.Controls.Add(_regInlineEdit);
            }

            _regInlineEdit.BackColor = _fprList.BackColor;
            _regInlineEdit.ForeColor = _fprList.ForeColor;
            _regInlineEdit.Text = value;
            _regInlineEdit.Location = new Point(x, y);
            _regInlineEdit.Size = new Size(w, h);
            _regInlineEdit.Visible = true;
            _regInlineEdit.Focus();
            _regInlineEdit.SelectAll();
        }

        private void UpdateRegisterListColumns(VirtualDisasmList lv, bool preserveFirstColumn = false)
        {
            if (lv.Columns.Count < 2)
                return;

            if (!preserveFirstColumn)
            {
                int regWidth = 44;
                foreach (var row in _fprRows)
                {
                    int measured = TextRenderer.MeasureText(row.Reg, _mono, new Size(9999, Math.Max(18, _mono.Height + 4)),
                        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + 10;
                    if (measured > regWidth)
                        regWidth = measured;
                }
                lv.Columns[0].Width = regWidth;
            }

            lv.Columns[1].Width = Math.Max(80, lv.ClientSize.Width - lv.Columns[0].Width - (lv.HasVerticalScrollbar ? 14 : 0));
        }

        private bool IsBreakpointSidebarActuallyVisible()
        {
            return _disasmBreakpointSplit != null &&
                   !_disasmBreakpointSplit.Panel2Collapsed &&
                   _breakpointsPanel != null &&
                   _breakpointsPanel.Visible &&
                   ReferenceEquals(_breakpointsPanel.Parent, _disasmBreakpointSplit.Panel2) &&
                   _disasmBreakpointSplit.Panel2.ClientSize.Width > 0;
        }

        private void SyncBreakpointSidebarMenuCheckState(bool visible)
        {
            if (_miBreakpointsSidebar != null && _miBreakpointsSidebar.Checked != visible)
                _miBreakpointsSidebar.Checked = visible;
        }

        private void ToggleBreakpointSidebarFromMenu()
        {
            SetBreakpointSidebarVisible(!IsBreakpointSidebarActuallyVisible(), refreshDebugger: true);
        }

        private void EnsureBreakpointSidebarVisible(bool refreshDebugger = false)
        {
            if (IsBreakpointSidebarActuallyVisible())
            {
                SyncBreakpointSidebarMenuCheckState(true);
                return;
            }

            SetBreakpointSidebarVisible(true, refreshDebugger);
        }

        private void SetBreakpointSidebarVisible(bool visible, bool refreshDebugger = true)
        {
            if (_disasmBreakpointSplit == null)
            {
                SyncBreakpointSidebarMenuCheckState(false);
                return;
            }

            if (visible)
            {
                const int WM_SETREDRAW = 0x000B;
                NativeMethods.SendMessage(_disasmBreakpointSplit.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
                _disasmBreakpointSplit.SuspendLayout();
                try
                {
                    if (_breakpointsPanel != null)
                    {
                        ApplyThemeToControlTree(_breakpointsPanel);
                        ApplyScrollbarTheme(_breakpointsPanel, _currentTheme == AppTheme.Dark);
                    }

                    // Pre-set splitter constraints before uncollapsing
                    _disasmBreakpointSplit.Panel1MinSize = 0;
                    _disasmBreakpointSplit.Panel2MinSize = 0;

                    _disasmBreakpointSplit.Panel2Collapsed = false;

                    // Force the sidebar to its fixed width immediately
                    {
                        int w = Math.Max(0, _disasmBreakpointSplit.ClientSize.Width);
                        int sw = _disasmBreakpointSplit.SplitterWidth;
                        int target = Math.Max(0, w - BreakpointSidebarFixedWidth - sw);
                        target = Math.Max(target, BreakpointSidebarMainMinWidth);
                        _disasmBreakpointSplit.SplitterDistance = Math.Min(target, Math.Max(0, w - sw));
                    }

                    ApplyBreakpointSidebarSplitConstraints();
                    MaintainBreakpointSidebarWidth();
                }
                finally
                {
                    _disasmBreakpointSplit.ResumeLayout(true);
                    NativeMethods.SendMessage(_disasmBreakpointSplit.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                    _disasmBreakpointSplit.Invalidate(true);
                    _disasmBreakpointSplit.Update();
                }
                SyncBreakpointSidebarMenuCheckState(IsBreakpointSidebarActuallyVisible());
                if (refreshDebugger)
                    RefreshDebuggerUiTick(force: true);
                return;
            }

            _disasmBreakpointSplit.Panel2Collapsed = true;
            SyncBreakpointSidebarMenuCheckState(false);
        }

        private void ApplyBreakpointSidebarSplitConstraints()
        {
            if (_disasmBreakpointSplit == null)
                return;

            int width = Math.Max(0, _disasmBreakpointSplit.ClientSize.Width);
            if (width <= 0 || !_disasmBreakpointSplit.IsHandleCreated || FindForm()?.WindowState == FormWindowState.Minimized)
                return;
            int splitterWidth = _disasmBreakpointSplit.SplitterWidth;
            int usableWidth = Math.Max(0, width - splitterWidth);

            int panel1Min = Math.Min(BreakpointSidebarMainMinWidth, usableWidth);
            int remainingForPanel2 = Math.Max(0, usableWidth - panel1Min);
            int panel2Min = Math.Min(BreakpointSidebarPanelMinWidth, remainingForPanel2);

            _disasmBreakpointSplit.Panel1MinSize = 0;
            _disasmBreakpointSplit.Panel2MinSize = 0;

            int minDistance = panel1Min;
            int maxDistance = Math.Max(minDistance, width - panel2Min - splitterWidth);
            int currentDistance = Math.Max(0, _disasmBreakpointSplit.SplitterDistance);
            int clampedDistance = Math.Min(Math.Max(currentDistance, minDistance), maxDistance);
            _disasmBreakpointSplit.SplitterDistance = clampedDistance;

            _disasmBreakpointSplit.Panel1MinSize = panel1Min;
            _disasmBreakpointSplit.Panel2MinSize = panel2Min;
        }

        private void CaptureBreakpointSidebarWidthFromSplitter()
        {
            if (_disasmBreakpointSplit == null || _disasmBreakpointSplit.Panel2Collapsed || _updatingBreakpointSidebarSplitter)
                return;

            int width = Math.Max(0, _disasmBreakpointSplit.ClientSize.Width);
            int sidebarWidth = Math.Max(0, width - _disasmBreakpointSplit.SplitterDistance - _disasmBreakpointSplit.SplitterWidth);
            if (sidebarWidth <= 0)
                return;

            _breakpointSidebarPreferredWidth = Math.Max(BreakpointSidebarPanelMinWidth, sidebarWidth);
        }

        private int GetClampedBreakpointSidebarPreferredWidth()
        {
            if (_disasmBreakpointSplit == null)
                return BreakpointSidebarFixedWidth;

            int width = Math.Max(0, _disasmBreakpointSplit.ClientSize.Width);
            int splitterWidth = _disasmBreakpointSplit.SplitterWidth;
            int usableWidth = Math.Max(0, width - splitterWidth);
            int panel1Min = Math.Min(BreakpointSidebarMainMinWidth, usableWidth);
            int maxSidebarWidth = Math.Max(0, usableWidth - panel1Min);
            if (maxSidebarWidth <= 0)
                return 0;

            int preferred = Math.Max(BreakpointSidebarPanelMinWidth, _breakpointSidebarPreferredWidth);
            return Math.Min(preferred, maxSidebarWidth);
        }

        private void MaintainBreakpointSidebarWidth()
        {
            if (_disasmBreakpointSplit == null)
                return;

            if (FindForm()?.WindowState == FormWindowState.Minimized)
                return;

            ApplyBreakpointSidebarSplitConstraints();

            if (_disasmBreakpointSplit.Panel2Collapsed)
                return;

            int width = Math.Max(0, _disasmBreakpointSplit.ClientSize.Width);
            int splitterWidth = _disasmBreakpointSplit.SplitterWidth;
            int desiredSidebarWidth = GetClampedBreakpointSidebarPreferredWidth();
            int minDistance = _disasmBreakpointSplit.Panel1MinSize;
            int maxDistance = Math.Max(minDistance, width - _disasmBreakpointSplit.Panel2MinSize - splitterWidth);
            int desiredDistance = Math.Max(minDistance, width - desiredSidebarWidth - splitterWidth);
            desiredDistance = Math.Min(desiredDistance, maxDistance);

            int currentDistance = Math.Max(0, _disasmBreakpointSplit.SplitterDistance);
            if (desiredDistance == currentDistance)
                return;

            _updatingBreakpointSidebarSplitter = true;
            try
            {
                _disasmBreakpointSplit.SplitterDistance = desiredDistance;
            }
            finally
            {
                _updatingBreakpointSidebarSplitter = false;
            }
        }

        private bool NeedDebuggerPolling()
        {
            return _userBreakpoints.Count > 0 ||
                   _readMemcheckAddress.HasValue ||
                   _writeMemcheckAddress.HasValue ||
                   _accessMonitorActive ||
                   (_miBreakpointsSidebar?.Checked ?? false) ||
                   _lastDebuggerPaused;
        }

        private bool EnsureDebugServerConnected(bool forceRetry = false)
        {
            if (!AllowLiveDebugClientConnections())
                return false;

            if (_debugServer.IsConnected)
            {
                _debugServerAvailable = true;
                UpdatePineDebugWindowStatus();
                return true;
            }
            if (!forceRetry && DateTime.UtcNow < _nextDebugServerRetryUtc)
                return false;

            try
            {
                _debugServer.Connect();
                var status = _debugServer.GetStatus();
                _debugServerAvailable = status.Alive || _debugServer.IsConnected;
                _nextDebugServerRetryUtc = DateTime.MinValue;
                UpdatePineDebugWindowStatus();
                return true;
            }
            catch
            {
                _debugServerAvailable = false;
                _nextDebugServerRetryUtc = DateTime.UtcNow.AddSeconds(3);
                try { _debugServer.Disconnect(); } catch { }
                UpdatePineDebugWindowStatus();
                return false;
            }
        }

        private void RefreshDebuggerUiTick(bool force = false)
        {
            if (!AllowLiveDebugClientConnections())
            {
                if (!_breakpointUiFrozen)
                    ClearPausedBreakpointUiState();
                ClearPausedBreakpointMenuStatus();
                _lastDebuggerPaused = false;
                _activeBreakpointAddress = null;
                _activeBreakpointIsWatchpoint = false;
                _disasmList?.Invalidate();
                return;
            }

            if (!force && _breakpointUiFrozen && _breakpointUiFrozenAddress.HasValue)
            {
                _debugServerAvailable = true;
                _lastDebuggerPaused = true;
                _activeBreakpointAddress = _breakpointUiFrozenAddress;
                _activeBreakpointIsWatchpoint = _breakpointUiFrozenIsWatchpoint;
                ApplyPausedBreakpointMenuStatus(_breakpointUiFrozenAddress.Value);
                _disasmList?.Invalidate();
                return;
            }

            if (!force && !NeedDebuggerPolling())
                return;
            if (!force && DateTime.UtcNow < _nextDebuggerPollUtc)
                return;
            int debuggerPollIntervalMs = (_accessMonitorActive && _accessMonitorPassiveMode)
                ? 75
                : DebuggerPollIntervalMs;
            _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(debuggerPollIntervalMs);

            if (!EnsureDebugServerConnected(forceRetry: force))
            {
                _debugServerAvailable = false;
                if (_breakpointUiFrozen && _breakpointUiFrozenAddress.HasValue)
                {
                    _lastDebuggerPaused = true;
                    _activeBreakpointAddress = _breakpointUiFrozenAddress;
                    _activeBreakpointIsWatchpoint = _breakpointUiFrozenIsWatchpoint;
                    ApplyPausedBreakpointMenuStatus(_breakpointUiFrozenAddress.Value);
                }
                else
                {
                    ClearPausedBreakpointUiState();
                    ClearPausedBreakpointMenuStatus();
                }
                _disasmList?.Invalidate();
                return;
            }

            try
            {
                var status = _debugServer.GetStatus();

                // Avoid polling breakpoint/memcheck lists while the VM is running.
                // Some PCSX2/debug-server builds momentarily pause/resume the VM while
                // enumerating debugger breakpoints. Keep the local breakpoint model as
                // authoritative while running; only sync from the server when already
                // paused or when a caller explicitly forces a refresh.
                IReadOnlyList<DebugBreakpointInfo> breakpointInfos = Array.Empty<DebugBreakpointInfo>();
                bool breakpointListSynced = false;
                if (status.Paused || force)
                {
                    try
                    {
                        if (_userBreakpoints.Count > 0 || force)
                        {
                            breakpointInfos = _debugServer.ListBreakpoints();
                            breakpointListSynced = true;
                        }
                    }
                    catch
                    {
                        breakpointInfos = Array.Empty<DebugBreakpointInfo>();
                    }
                }

                bool wasPaused = _lastDebuggerPaused;
                uint previousPc = _lastDebuggerPc;
                uint? previousActive = _activeBreakpointAddress;

                if (breakpointListSynced)
                    SyncUserBreakpointsFromServer(breakpointInfos);

                bool readWatchEnabled = !_watchpointsSuspended && _chkReadBreakpoint?.Checked == true && _readMemcheckAddress.HasValue;
                bool writeWatchEnabled = !_watchpointsSuspended && _chkWriteBreakpoint?.Checked == true && _writeMemcheckAddress.HasValue;

                uint normalizedStatusPc = NormalizeMipsAddress(status.Pc);
                uint? hitAddress = null;
                bool hitIsWatchpoint = false;

                if (_accessMonitorActive && _accessMonitorPassiveMode)
                {
                    PollAccessMonitorMemcheck();
                }
                else if (_accessMonitorActive && !_accessMonitorPassiveMode)
                {
                    RearmAccessMonitorIfNeeded(status);
                }

                // ── Access Monitor: in fallback break/resume mode, record the accessing PC
                //    and immediately resume. Passive mode uses memcheck hit polling instead.
                if (_accessMonitorActive && !_accessMonitorPassiveMode && _accessMonitorMemcheckInstalled && status.Paused && status.Pc != 0
                    && !_userBreakpoints.Contains(normalizedStatusPc))
                {
                    // Read $ra (return address) while paused for the Parent column
                    uint parentRa = 0;
                    if (!_accessMonitorParents.ContainsKey(normalizedStatusPc))
                    {
                        try
                        {
                            var regs = _debugServer.ReadRegisters();
                            if (TryGetGeneralRegisterValue(regs, "ra", out uint raVal) && raVal != 0)
                                parentRa = raVal;
                        }
                        catch { /* non-fatal — parent will be 0 */ }
                    }

                    RecordAccessMonitorHit(normalizedStatusPc, 1, parentRa);
                    UpdateAccessMonitorBreakThrottle(normalizedStatusPc);
                    try { _debugServer.Resume(); } catch { }
                    if (DateTime.UtcNow >= _accessMonitorNextUiRefresh)
                    {
                        RefreshAccessMonitorList();
                        _accessMonitorNextUiRefresh = DateTime.UtcNow.AddMilliseconds(150);
                    }
                    _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(_accessMonitorNeedsRearm ? 8 : 10);
                    _lastDebuggerPaused = false;
                    return;
                }

                if (status.Paused && _userBreakpoints.Contains(normalizedStatusPc))
                {
                    hitAddress = status.Pc;
                }
                else if (status.Paused && status.Pc != 0 && (readWatchEnabled || writeWatchEnabled))
                {
                    // Native memchecks already paused the VM. Treat any other paused state with armed
                    // watchpoints as a watchpoint hit and anchor the UI to the current PC.
                    hitAddress = status.Pc;
                    hitIsWatchpoint = true;
                }

                if (hitAddress.HasValue)
                {
                    LatchPausedBreakpointUiState(hitAddress.Value, hitIsWatchpoint);
                    FreezePausedBreakpointUi(hitAddress.Value, hitIsWatchpoint);
                }

                bool watchStillLooksTriggered = status.Paused && (readWatchEnabled || writeWatchEnabled);

                bool keepLatchedPause = false;
                if (_pausedBreakpointUiLatched && _pausedBreakpointUiAddress.HasValue)
                {
                    if (status.Paused)
                    {
                        keepLatchedPause = true;
                        _pausedBreakpointUiRunningPolls = 0;
                    }
                    else if (_pausedBreakpointUiIsWatchpoint && watchStillLooksTriggered)
                    {
                        keepLatchedPause = true;
                        _pausedBreakpointUiRunningPolls = 0;
                    }
                    else
                    {
                        bool definitelyRunning = status.Cycles != _lastDebuggerCycles || status.Pc != _lastDebuggerPc;
                        if (definitelyRunning)
                            _pausedBreakpointUiRunningPolls++;
                        else
                            _pausedBreakpointUiRunningPolls = 0;

                        keepLatchedPause = _pausedBreakpointUiRunningPolls < 2;
                        if (!keepLatchedPause)
                            ClearPausedBreakpointUiState();
                    }
                }

                uint? activeAddress = hitAddress;
                bool activeIsWatchpoint = hitIsWatchpoint;
                if (!activeAddress.HasValue && keepLatchedPause && _pausedBreakpointUiAddress.HasValue)
                {
                    activeAddress = _pausedBreakpointUiAddress.Value;
                    activeIsWatchpoint = _pausedBreakpointUiIsWatchpoint;
                }

                bool frozenPause = _breakpointUiFrozen && _breakpointUiFrozenAddress.HasValue;
                if (frozenPause && _breakpointUiFrozenAddress.HasValue)
                {
                    activeAddress = _breakpointUiFrozenAddress.Value;
                    activeIsWatchpoint = _breakpointUiFrozenIsWatchpoint;
                }

                bool effectivePaused = frozenPause || status.Paused || (keepLatchedPause && activeAddress.HasValue);

                _debugServerAvailable = true;
                _lastDebuggerPaused = effectivePaused;
                _lastDebuggerPc = status.Pc;
                _lastDebuggerCycles = status.Cycles;
                _activeBreakpointAddress = activeAddress;
                _activeBreakpointIsWatchpoint = effectivePaused && activeAddress.HasValue && activeIsWatchpoint;
                if (effectivePaused && activeAddress.HasValue)
                    ApplyPausedBreakpointMenuStatus(activeAddress.Value);
                else if (!_breakpointUiFrozen)
                    ClearPausedBreakpointMenuStatus();

                bool stateChanged = effectivePaused != wasPaused || status.Pc != previousPc || activeAddress != previousActive;
                if (effectivePaused && stateChanged)
                {
                    if (activeAddress.HasValue)
                        EnsureBreakpointSidebarVisible(refreshDebugger: false);
                    RefreshPausedDebuggerDetails();
                    FocusDebuggerAddress(activeAddress ?? _pausedBreakpointUiAddress ?? status.Pc);
                }
                else if (!effectivePaused && wasPaused && !_breakpointUiFrozen)
                {
                    _activeBreakpointAddress = null;
                    _activeBreakpointIsWatchpoint = false;
                }

                _disasmList?.Invalidate();
            }
            catch
            {
                _debugServerAvailable = false;
                if (!_breakpointUiFrozen)
                    ClearPausedBreakpointUiState();
                try { _debugServer.Disconnect(); } catch { }
                _nextDebugServerRetryUtc = DateTime.UtcNow.AddSeconds(3);
                if (_breakpointUiFrozen && _breakpointUiFrozenAddress.HasValue)
                {
                    _lastDebuggerPaused = true;
                    _activeBreakpointAddress = _breakpointUiFrozenAddress;
                    _activeBreakpointIsWatchpoint = _breakpointUiFrozenIsWatchpoint;
                    ApplyPausedBreakpointMenuStatus(_breakpointUiFrozenAddress.Value);
                }
                else
                {
                    ClearPausedBreakpointMenuStatus();
                }
                _disasmList?.Invalidate();
            }
        }

        private static DebugMemcheckInfo? FindTrackedMemcheck(IReadOnlyList<DebugMemcheckInfo> memchecks, uint? startAddress)
        {
            if (!startAddress.HasValue)
                return null;
            return memchecks.FirstOrDefault(mc => mc.Start == startAddress.Value ||
                                                  (startAddress.Value >= mc.Start && startAddress.Value < mc.End));
        }

        private static uint GetWatchpointEndAddress(uint address)
        {
            return TryBuildWatchpointRange(address, out _, out uint endExclusive)
                ? endExclusive
                : NormalizeMipsAddress(address);
        }

        private static bool IsPs2DisDebugArtifact(string? description)
        {
            return !string.IsNullOrWhiteSpace(description) &&
                   description.TrimStart().StartsWith("ps2dis#", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryBuildEeRamRange(uint address, uint byteCount, out uint normalizedAddress, out uint endExclusive)
        {
            normalizedAddress = NormalizeMipsAddress(address);
            endExclusive = normalizedAddress;

            if (byteCount == 0 || normalizedAddress >= EeRamSizeBytes)
                return false;

            ulong end = (ulong)normalizedAddress + byteCount;
            if (end > EeRamSizeBytes)
                return false;

            endExclusive = (uint)end;
            return endExclusive > normalizedAddress;
        }

        private static bool TryNormalizeInstructionBreakpoint(uint address, out uint normalizedAddress)
        {
            normalizedAddress = NormalizeMipsAddress(address);
            return (normalizedAddress & 3u) == 0 && TryBuildEeRamRange(normalizedAddress, 4u, out normalizedAddress, out _);
        }

        private static bool TryBuildWatchpointRange(uint address, out uint normalizedAddress, out uint endExclusive)
            => TryBuildEeRamRange(address, WatchpointSizeBytes, out normalizedAddress, out endExclusive);

        private bool TryBuildAccessMonitorRange(uint address, out uint normalizedAddress, out uint endExclusive)
            => TryBuildEeRamRange(address, Math.Max(1u, _accessMonitorSizeBytes), out normalizedAddress, out endExclusive);

        private bool TryPauseDebugServerForMutation(string operationName, out bool resumeAfterMutation, out string? errorMessage)
        {
            resumeAfterMutation = false;
            errorMessage = null;

            try
            {
                var status = _debugServer.GetStatus();
                if (status.Paused)
                    return true;

                resumeAfterMutation = true;
                _debugServer.Pause();

                for (int i = 0; i < 50; i++)
                {
                    Thread.Sleep(10);
                    try
                    {
                        if (_debugServer.GetStatus().Paused)
                            return true;
                    }
                    catch
                    {
                        // Keep waiting briefly; transient status reads can fail while PCSX2 is pausing.
                    }
                }

                errorMessage = $"Timed out pausing PCSX2 before {operationName}. The breakpoint was not changed to avoid an emulator crash.";
                if (resumeAfterMutation)
                {
                    try { _debugServer.Resume(); } catch { }
                    resumeAfterMutation = false;
                }
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Could not pause PCSX2 before {operationName}: {ex.Message}";
                if (resumeAfterMutation)
                {
                    try { _debugServer.Resume(); } catch { }
                    resumeAfterMutation = false;
                }
                return false;
            }
        }

        private void ResumeDebugServerAfterMutation(bool resumeAfterMutation)
        {
            if (!resumeAfterMutation)
                return;

            try { _debugServer.Resume(); } catch { }
        }

        private void SyncDesiredBreakpointsToServer()
        {
            // Do not use the debug server's global clear_breakpoints command here.
            // Some PCSX2/debug-server builds implement that command by pausing and
            // resuming the VM. Reconcile ps2dis#-owned items with per-item commands,
            // but do all listing/removal/addition while the EE is paused.
            var desiredPcBreakpoints = new HashSet<uint>();
            foreach (uint userBreakpoint in _userBreakpoints)
            {
                if (TryNormalizeInstructionBreakpoint(userBreakpoint, out uint normalizedBreakpoint))
                    desiredPcBreakpoints.Add(normalizedBreakpoint);
            }

            // Always reconcile memchecks when this helper is called. A checkbox may
            // already have cleared its local address before calling us, but the old
            // ps2dis# memcheck can still be installed in PCSX2 and must be removed.
            if (!TryPauseDebugServerForMutation("reconciling breakpoints/watchpoints", out bool resumeAfterBatchMutation, out string? pauseError))
                throw new IOException(pauseError ?? "Could not pause PCSX2 before changing breakpoints/watchpoints.");

            try
            {
                IReadOnlyList<DebugBreakpointInfo> serverPcBreakpoints = Array.Empty<DebugBreakpointInfo>();
                try
                {
                    serverPcBreakpoints = _debugServer.ListBreakpoints();
                }
                catch
                {
                    // Listing is best-effort. If it fails, still try to arm desired breakpoints below.
                }

                var installedAppPcBreakpoints = new HashSet<uint>();
                foreach (DebugBreakpointInfo bp in serverPcBreakpoints)
                {
                    if (bp.Temporary || bp.Stepping)
                        continue;

                    uint serverAddress = NormalizeMipsAddress(bp.Address);
                    if (IsPs2DisDebugArtifact(bp.Description))
                        installedAppPcBreakpoints.Add(serverAddress);
                }

                foreach (uint serverAddress in installedAppPcBreakpoints)
                {
                    if (!desiredPcBreakpoints.Contains(serverAddress))
                    {
                        try { _debugServer.RemoveBreakpoint(serverAddress); } catch { }
                    }
                }

                foreach (uint bp in desiredPcBreakpoints.OrderBy(a => a))
                {
                    if (!installedAppPcBreakpoints.Contains(bp))
                        _debugServer.SetBreakpoint(bp, description: $"ps2dis# {bp:X8}");
                }

                try
                {
                    foreach (DebugMemcheckInfo mc in _debugServer.ListMemchecks())
                    {
                        if (IsPs2DisDebugArtifact(mc.Description))
                        {
                            try { _debugServer.RemoveMemcheck(mc.Start, mc.End); } catch { }
                        }
                    }
                }
                catch
                {
                    // If listing memchecks fails, fall back to removing the addresses we know about.
                    if (_readMemcheckAddress.HasValue)
                        try { _debugServer.RemoveMemcheck(_readMemcheckAddress.Value, GetWatchpointEndAddress(_readMemcheckAddress.Value)); } catch { }
                    if (_writeMemcheckAddress.HasValue)
                        try { _debugServer.RemoveMemcheck(_writeMemcheckAddress.Value, GetWatchpointEndAddress(_writeMemcheckAddress.Value)); } catch { }
                    if (_accessMonitorAddressValid)
                        try { _debugServer.RemoveMemcheck(_accessMonitorAddress, GetAccessMonitorEndAddress(_accessMonitorAddress)); } catch { }
                }

                _accessMonitorMemcheckInstalled = false;

                if (_chkReadBreakpoint?.Checked == true && _readMemcheckAddress.HasValue &&
                    TryBuildWatchpointRange(_readMemcheckAddress.Value, out uint readStart, out uint readEnd))
                {
                    _debugServer.SetMemcheck(readStart, readEnd, "read",
                        action: "break", description: "ps2dis# OnRead");
                }

                if (_chkWriteBreakpoint?.Checked == true && _writeMemcheckAddress.HasValue &&
                    TryBuildWatchpointRange(_writeMemcheckAddress.Value, out uint writeStart, out uint writeEnd))
                {
                    _debugServer.SetMemcheck(writeStart, writeEnd, "write",
                        action: "break", description: "ps2dis# OnWrite");
                }

                if (_accessMonitorActive && TryBuildAccessMonitorRange(_accessMonitorAddress, out uint accessStart, out uint accessEnd))
                {
                    _debugServer.SetMemcheck(accessStart, accessEnd, _accessMonitorType,
                        action: "break", description: "ps2dis# AccessMonitor");
                    _accessMonitorMemcheckInstalled = true;
                }
            }
            finally
            {
                ResumeDebugServerAfterMutation(resumeAfterBatchMutation);
            }

            _watchpointsSuspended = false;
            _readMemcheckHits = -1;
            _writeMemcheckHits = -1;
            _nextDebuggerPollUtc = DateTime.MinValue;
        }

        private void RemoveAllDebugBreakpointsWithoutGlobalClear()
        {
            // Do not call list_breakpoints/list_memchecks here. On affected PCSX2
            // builds those list helpers are a hidden pause/resume source. Clear only
            // debugger artifacts that ps2dis# itself knows it installed, and pause
            // once around all PC breakpoint and memcheck removals.
            bool hasDebuggerArtifacts = _userBreakpoints.Count > 0 ||
                                        _readMemcheckAddress.HasValue ||
                                        _writeMemcheckAddress.HasValue ||
                                        _accessMonitorAddressValid;
            bool resumeAfterMutation = false;
            if (hasDebuggerArtifacts &&
                !TryPauseDebugServerForMutation("clearing breakpoints/watchpoints", out resumeAfterMutation, out string? pauseError))
            {
                throw new IOException(pauseError ?? "Could not pause PCSX2 before clearing breakpoints/watchpoints.");
            }

            try
            {
                foreach (uint bp in _userBreakpoints.ToArray())
                {
                    try { _debugServer.RemoveBreakpoint(NormalizeMipsAddress(bp)); } catch { }
                }

                if (_readMemcheckAddress.HasValue)
                    try { _debugServer.RemoveMemcheck(_readMemcheckAddress.Value, GetWatchpointEndAddress(_readMemcheckAddress.Value)); } catch { }
                if (_writeMemcheckAddress.HasValue)
                    try { _debugServer.RemoveMemcheck(_writeMemcheckAddress.Value, GetWatchpointEndAddress(_writeMemcheckAddress.Value)); } catch { }
                if (_accessMonitorAddressValid)
                    try { _debugServer.RemoveMemcheck(_accessMonitorAddress, GetAccessMonitorEndAddress(_accessMonitorAddress)); } catch { }
            }
            finally
            {
                ResumeDebugServerAfterMutation(resumeAfterMutation);
            }
        }

        private uint GetAccessMonitorEndAddress(uint address)
        {
            return TryBuildAccessMonitorRange(address, out _, out uint endExclusive)
                ? endExclusive
                : NormalizeMipsAddress(address);
        }

        private bool SuspendWatchpoints()
        {
            if (_watchpointsSuspended)
                return true;

            bool hadWatchpoints = false;
            var removedAddresses = new HashSet<uint>();

            if (_readMemcheckAddress.HasValue)
            {
                hadWatchpoints = true;
                removedAddresses.Add(_readMemcheckAddress.Value);
            }

            if (_writeMemcheckAddress.HasValue)
            {
                hadWatchpoints = true;
                removedAddresses.Add(_writeMemcheckAddress.Value);
            }

            foreach (uint address in removedAddresses)
            {
                try { _debugServer.RemoveMemcheck(address, GetWatchpointEndAddress(address)); } catch { }
            }

            if (hadWatchpoints)
            {
                _watchpointsSuspended = true;
                _readMemcheckHits = -1;
                _writeMemcheckHits = -1;
            }

            return hadWatchpoints;
        }

        private void ResumeWatchpoints()
        {
            if (!_watchpointsSuspended)
                return;

            if (_readMemcheckAddress.HasValue && TryBuildWatchpointRange(_readMemcheckAddress.Value, out uint readStart, out uint readEnd))
            {
                try
                {
                    _debugServer.SetMemcheck(readStart, readEnd, "read",
                        action: "break", description: "ps2dis# OnRead");
                }
                catch { }
            }

            if (_writeMemcheckAddress.HasValue && TryBuildWatchpointRange(_writeMemcheckAddress.Value, out uint writeStart, out uint writeEnd))
            {
                try
                {
                    _debugServer.SetMemcheck(writeStart, writeEnd, "write",
                        action: "break", description: "ps2dis# OnWrite");
                }
                catch { }
            }

            _watchpointsSuspended = false;
            _readMemcheckHits = -1;
            _writeMemcheckHits = -1;
            _nextDebuggerPollUtc = DateTime.MinValue;
        }

        private void SyncUserBreakpointsFromServer(IReadOnlyList<DebugBreakpointInfo> breakpointInfos)
        {
            _userBreakpoints.Clear();
            foreach (DebugBreakpointInfo bp in breakpointInfos)
            {
                if (!bp.Enabled || bp.Temporary || bp.Stepping || !IsPs2DisDebugArtifact(bp.Description))
                    continue;
                if (TryNormalizeInstructionBreakpoint(bp.Address, out uint normalizedAddress))
                    _userBreakpoints.Add(normalizedAddress);
            }
        }

        private void RefreshPausedDebuggerDetails()
        {
            try
            {
                DebugRegisterSnapshot regs = _debugServer.ReadRegisters();
                IReadOnlyList<DebugBacktraceFrame> frames = _debugServer.GetBacktrace();
                // Cache the snapshot for the live break-time annotations on the
                // rows around the active break PC. Cleared in
                // ClearPausedBreakpointUiState / ContinueExecution / step.
                _breakRegisterSnapshot = regs;
                PopulateRegisters(regs);
                PopulateCallStack(frames, regs);
            }
            catch
            {
                // Non-fatal: keep the existing data visible.
            }
        }

        private void FocusDebuggerAddress(uint address)
        {
            if (address == 0)
                return;

            // Normalize kernel/segment addresses (e.g. 0x80000280 → 0x00000280)
            address = NormalizeMipsAddress(address);

            if (_mainTabs != null && _mainTabs.Pages.Count > 0)
                _mainTabs.SelectedIndex = 0;

            EnsureBreakpointSidebarVisible(refreshDebugger: false);

            if (TryGetRowIndexByAddress(address, out int idx))
            {
                // Only center the view if the row isn't already visible
                bool visible = IsRowVisible(idx);
                SelectRow(idx, center: !visible);
                return;
            }

            // Do not fall back to the nearest row for debugger breaks.  When PCSX2
            // reports a PC outside the currently materialized disassembly window,
            // selecting the nearest row makes it look like ps2dis# jumped without
            // highlighting the actual break.  Build a small window around the PC so
            // the pending navigation can resolve to the exact aligned instruction row.
            if (TryStartDebuggerDisassemblyWindow(address))
                return;

            SetStatusText($"Breakpoint PC {address:X8} is outside the loaded EE RAM image.");
        }

        private bool TryStartDebuggerDisassemblyWindow(uint normalizedAddress)
        {
            if (_fileData == null || _fileData.Length == 0)
                return false;

            uint alignedAddress = normalizedAddress & ~3u;
            if (!TryBuildEeRamRange(alignedAddress, 4u, out alignedAddress, out _))
                return false;

            uint normalizedBase = NormalizeMipsAddress(_baseAddr);
            ulong normalizedEnd = (ulong)normalizedBase + (uint)_fileData.Length;
            if (alignedAddress < normalizedBase || (ulong)alignedAddress + 4u > normalizedEnd)
                return false;

            uint fileOffset = alignedAddress - normalizedBase;
            uint windowStartOffset = fileOffset & ~0xFFFFu;
            uint remaining = (uint)Math.Max(0, _fileData.Length - (int)Math.Min(windowStartOffset, (uint)_fileData.Length));
            if (remaining == 0)
                return false;

            const uint DebuggerWindowLength = 0x00040000u; // 256 KB around the break PC.
            _disasmBase = _baseAddr + windowStartOffset;
            _disasmLen = Math.Min(DebuggerWindowLength, remaining);
            _pendingNavAddr = _baseAddr + fileOffset;
            _pendingNavVisibleOffset = 0;
            _pendingNavCenter = true;
            StartDisassembly();
            return true;
        }


        private static bool TryParseEightHex(string? text, out uint value)
        {
            value = 0;
            string raw = (text ?? string.Empty).Trim();
            if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                raw = raw[2..];
            if (raw.Length == 0 || raw.Length > 8)
                return false;
            return uint.TryParse(raw, System.Globalization.NumberStyles.HexNumber, null, out value);
        }

        private void SetBreakpointOnSelectedRow()
        {
            if (_selRow < 0 || _selRow >= _rows.Count)
                return;

            uint address = _rows[_selRow].Address;
            if (!TryNormalizeInstructionBreakpoint(address, out address))
            {
                MessageBox.Show("Instruction breakpoints require a 4-byte aligned EE RAM address.", "Breakpoints", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!EnsureDebugServerConnected(forceRetry: true))
            {
                MessageBox.Show($"Breakpoint control requires a PCSX2 build exposing the optional debug server on port {_debugServer.Port}. Plain PINE access is not enough for stepping or breakpoints.", "Breakpoints", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearPausedBreakpointMenuStatus();
                return;
            }

            try
            {
                if (_userBreakpoints.Contains(address))
                    return;

                // Mutating PCSX2 breakpoint tables while the EE recompiler is running
                // can tear down JIT state out from under the VM. Always quiesce the
                // EE before adding/removing debugger artifacts, and fail closed if the
                // pause cannot be confirmed.
                if (!TryPauseDebugServerForMutation("setting the PC breakpoint", out bool resumeAfterMutation, out string? pauseError))
                    throw new IOException(pauseError ?? "Could not pause PCSX2 before setting the breakpoint.");

                try
                {
                    _debugServer.SetBreakpoint(address, description: $"ps2dis# {address:X8}");
                    _userBreakpoints.Add(address);
                }
                finally
                {
                    ResumeDebugServerAfterMutation(resumeAfterMutation);
                }

                _disasmList?.Invalidate();
                // Let the periodic poll pick up the new state instead of
                // hammering the server with 3+ calls immediately.
                _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(300);
            }
            catch (Exception ex)
            {
                // Connection may have broken — clean up and show a non-fatal message.
                try { _debugServer.Disconnect(); } catch { }
                _debugServerAvailable = false;
                _nextDebugServerRetryUtc = DateTime.UtcNow.AddSeconds(2);
                MessageBox.Show($"Failed to set breakpoint: {ex.Message}\n\nThe debug server connection was reset. Try again.", "Set breakpoint", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetMemoryBreakpointOnSelectedRow(bool isRead)
        {
            if (_selRow < 0 || _selRow >= _rows.Count)
                return;

            TextBox? txt = isRead ? _txtReadBreakpoint : _txtWriteBreakpoint;
            CheckBox? chk = isRead ? _chkReadBreakpoint : _chkWriteBreakpoint;
            if (txt == null || chk == null)
                return;

            uint address = _rows[_selRow].Address;
            txt.Text = address.ToString("X8");

            if (!chk.Checked)
            {
                chk.Checked = true;
            }
            else
            {
                ApplyMemoryBreakpointInput(isRead, showErrors: true);
            }

            uint? armedAddress = isRead ? _readMemcheckAddress : _writeMemcheckAddress;
            if (chk.Checked && armedAddress.HasValue && armedAddress.Value == address)
            {
                try
                {
                    if (EnsureDebugServerConnected(forceRetry: false))
                    {
                        var status = _debugServer.GetStatus();
                        if (status.Paused)
                        {
                            _activeBreakpointAddress = address;
                            ApplyPausedBreakpointMenuStatus(address);
                            EnsureBreakpointSidebarVisible(refreshDebugger: false);
                        }
                    }
                }
                catch
                {
                    // Non-fatal: the periodic debugger poll will reconcile the UI.
                }
                _disasmList?.Invalidate();
            }
        }

        private void ClearAllBreakpoints()
        {
            try
            {
                bool hasKnownServerBreakpoints = _userBreakpoints.Count > 0 ||
                                                _readMemcheckAddress.HasValue ||
                                                _writeMemcheckAddress.HasValue ||
                                                _accessMonitorActive;
                if (hasKnownServerBreakpoints && EnsureDebugServerConnected(forceRetry: true))
                {
                    RemoveAllDebugBreakpointsWithoutGlobalClear();
                }
            }
            catch (Exception ex)
            {
                // Non-fatal — still clear local state below.
                try { _debugServer.Disconnect(); } catch { }
                _debugServerAvailable = false;
                _nextDebugServerRetryUtc = DateTime.UtcNow.AddSeconds(2);
            }

            _userBreakpoints.Clear();
            _readMemcheckAddress = null;
            _writeMemcheckAddress = null;
            _readMemcheckHits = -1;
            _writeMemcheckHits = -1;
            _watchpointsSuspended = false;
            StopAccessMonitor(quiet: true, resumeIfPaused: false);
            if (_chkReadBreakpoint != null) _chkReadBreakpoint.Checked = false;
            if (_chkWriteBreakpoint != null) _chkWriteBreakpoint.Checked = false;
            if (_txtReadBreakpoint != null) _txtReadBreakpoint.BackColor = _themeWindowBack;
            if (_txtWriteBreakpoint != null) _txtWriteBreakpoint.BackColor = _themeWindowBack;
            // Do NOT clear the frozen/paused breakpoint UI state or the menu status
            // here — the emulator is still paused. Only the Continue button should
            // dismiss the "PAUSED: BREAKPOINT" label and resume execution.
            _disasmList?.Invalidate();
        }

        private void ContinueExecution()
        {
            if (!EnsureDebugServerConnected(forceRetry: true))
                return;

            try
            {
                ClearFrozenBreakpointUi();
                _debugServer.Resume();
                ClearPausedBreakpointUiState();
                ClearPausedBreakpointMenuStatus();
                // Let the periodic poll pick up the new state after a brief delay.
                _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(300);
                _disasmList?.Invalidate();
            }
            catch (Exception ex)
            {
                try { _debugServer.Disconnect(); } catch { }
                _debugServerAvailable = false;
                _nextDebugServerRetryUtc = DateTime.UtcNow.AddSeconds(2);
                MessageBox.Show($"Resume failed: {ex.Message}", "Continue", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void StepExecution(bool stepOver)
        {
            if (!EnsureDebugServerConnected(forceRetry: true))
                return;
            try
            {
                var preStatus = _debugServer.GetStatus();
                if (!preStatus.Paused)
                    return;

                uint pc = preStatus.Pc;
                uint normalizedPc = NormalizeMipsAddress(pc);

                // Read the instruction word at the current PC to determine next addresses.
                // We read from live PINE data if available, otherwise fall back to file data.
                uint word = 0;
                bool gotWord = false;
                if (_pineAvailable && TryBuildEeRamRange(normalizedPc, 4u, out uint pineReadPc, out _))
                {
                    try
                    {
                        byte[] buf = _pine.ReadMemory(pineReadPc, 4);
                        if (buf != null && buf.Length >= 4)
                        {
                            word = BitConverter.ToUInt32(buf, 0);
                            gotWord = true;
                        }
                    }
                    catch { }
                }
                if (!gotWord && _fileData != null)
                {
                    long off = (long)normalizedPc - _baseAddr;
                    if (off >= 0 && off + 4 <= _fileData.Length)
                    {
                        word = BitConverter.ToUInt32(_fileData, (int)off);
                        gotWord = true;
                    }
                }

                if (!gotWord)
                {
                    // Cannot decode instruction — fall back to raw step command
                    FallbackStep(stepOver, pc);
                    return;
                }

                // Decode the instruction to find all possible next PCs
                var disasm = GetCachedDisasm();
                var (kind, target) = disasm.DecodeKindAndTarget(word, normalizedPc);

                var tempBreakpoints = new List<uint>();

                bool isBranchOrJump = kind is InstructionType.Branch or InstructionType.Jump;
                bool isCall = kind == InstructionType.Call;
                bool isRegisterJump = (word >> 26) == 0 && ((word & 0x3F) == 0x08 || (word & 0x3F) == 0x09); // jr, jalr

                if (stepOver && (isCall || (isRegisterJump && (word & 0x3F) == 0x09)))
                {
                    // Step Over a call: set breakpoint at return address (PC+8, after delay slot)
                    tempBreakpoints.Add(normalizedPc + 8);
                }
                else if (isBranchOrJump || isCall)
                {
                    // Branch/Jump: could go to target or fall through (PC+8, past delay slot)
                    tempBreakpoints.Add(normalizedPc + 8); // fall-through
                    if (target != 0)
                        tempBreakpoints.Add(NormalizeMipsAddress(target));
                    // For register jumps (jr/jalr), we must read the register to know the target
                    if (isRegisterJump)
                    {
                        uint rs = (word >> 21) & 0x1F;
                        bool gotRegTarget = false;
                        try
                        {
                            var regs = _debugServer.ReadRegisters();
                            uint? regAddr = FindGprAddress(regs, rs);
                            if (regAddr.HasValue)
                            {
                                tempBreakpoints.Add(NormalizeMipsAddress(regAddr.Value));
                                gotRegTarget = true;
                            }
                        }
                        catch { }
                        // If we couldn't resolve the register target, the temp breakpoint
                        // approach won't work — fall back to the raw step command
                        if (!gotRegTarget)
                        {
                            FallbackStep(stepOver, pc);
                            return;
                        }
                    }
                }
                else
                {
                    // Normal instruction: next PC is PC+4
                    tempBreakpoints.Add(normalizedPc + 4);
                }

                // Remove duplicates, invalid EE targets, and the current PC
                var uniqueTargets = new HashSet<uint>();
                foreach (uint targetAddress in tempBreakpoints)
                {
                    if (TryNormalizeInstructionBreakpoint(targetAddress, out uint normalizedTarget))
                        uniqueTargets.Add(normalizedTarget);
                }
                uniqueTargets.Remove(normalizedPc);

                if (uniqueTargets.Count == 0)
                {
                    FallbackStep(stepOver, pc);
                    return;
                }

                // Remove all user breakpoints to prevent them from interfering
                var savedBreakpoints = new List<uint>(_userBreakpoints);
                bool watchpointsSuspended = SuspendWatchpoints();
                try
                {
                    foreach (uint bp in savedBreakpoints)
                    {
                        try { _debugServer.RemoveBreakpoint(bp); } catch { }
                    }

                    // Set temporary breakpoints at all possible next PCs
                    foreach (uint addr in uniqueTargets)
                    {
                        try { _debugServer.SetBreakpoint(addr, temporary: true, description: "ps2dis# step"); } catch { }
                    }

                    ClearFrozenBreakpointUi();

                    // Resume — the emulator will run until it hits one of the temporary breakpoints
                    _debugServer.Resume();

                    // Wait briefly for the emulator to hit the temporary breakpoint
                    // (it should be nearly instant since we're stepping one instruction)
                    System.Threading.Thread.Sleep(50);

                    // Query the actual PC
                    uint newPc;
                    try
                    {
                        var postStatus = _debugServer.GetStatus();
                        // If not paused yet, wait a bit longer
                        if (!postStatus.Paused)
                        {
                            System.Threading.Thread.Sleep(100);
                            postStatus = _debugServer.GetStatus();
                        }
                        newPc = NormalizeMipsAddress(postStatus.Pc);
                    }
                    catch
                    {
                        newPc = 0;
                    }

                    if (newPc == 0)
                    {
                        _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(100);
                        RefreshDebuggerUiTick(force: true);
                        return;
                    }

                    FreezePausedBreakpointUi(newPc, isWatchpoint: false);
                    _lastDebuggerPc = newPc;
                    ApplyPausedBreakpointMenuStatus(newPc);
                    RefreshPausedDebuggerDetails();
                    FocusDebuggerAddress(newPc);
                    _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(400);
                    _disasmList?.Invalidate();
                    return;
                }
                finally
                {
                    // Clean up temporary breakpoints (they should auto-remove, but be safe)
                    foreach (uint addr in uniqueTargets)
                    {
                        try { _debugServer.RemoveBreakpoint(addr); } catch { }
                    }

                    // Restore all user breakpoints
                    foreach (uint bp in savedBreakpoints)
                    {
                        try { _debugServer.SetBreakpoint(bp, description: $"ps2dis# {bp:X8}"); } catch { }
                    }

                    if (watchpointsSuspended)
                        ResumeWatchpoints();
                }

            }
            catch (Exception ex)
            {
                try { _debugServer.Disconnect(); } catch { }
                _debugServerAvailable = false;
                _nextDebugServerRetryUtc = DateTime.UtcNow.AddSeconds(2);
                MessageBox.Show($"{(stepOver ? "Step Over" : "Step")} failed: {ex.Message}", stepOver ? "Step Over" : "Step", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Fallback step using the raw debug server step command when instruction decoding isn't possible.</summary>
        private void FallbackStep(bool stepOver, uint rawPc)
        {
            var savedBreakpoints = new List<uint>(_userBreakpoints);
            bool watchpointsSuspended = SuspendWatchpoints();
            try
            {
                foreach (uint bp in savedBreakpoints)
                {
                    try { _debugServer.RemoveBreakpoint(bp); } catch { }
                }

                ClearFrozenBreakpointUi();

                if (stepOver)
                    _debugServer.StepOver();
                else
                    _debugServer.Step();

                uint newPc;
                try
                {
                    var postStatus = _debugServer.GetStatus();
                    newPc = NormalizeMipsAddress(postStatus.Pc);
                }
                catch { newPc = 0; }

                if (newPc == 0)
                {
                    _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(100);
                    RefreshDebuggerUiTick(force: true);
                    return;
                }

                FreezePausedBreakpointUi(newPc, isWatchpoint: false);
                _lastDebuggerPc = newPc;
                ApplyPausedBreakpointMenuStatus(newPc);
                RefreshPausedDebuggerDetails();
                FocusDebuggerAddress(newPc);
                _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(400);
                _disasmList?.Invalidate();
                return;
            }
            finally
            {
                foreach (uint bp in savedBreakpoints)
                {
                    try { _debugServer.SetBreakpoint(bp, description: $"ps2dis# {bp:X8}"); } catch { }
                }

                if (watchpointsSuspended)
                    ResumeWatchpoints();
            }

        }

        /// <summary>Find the value of a GPR by index from a register snapshot, returned as a normalized uint address.</summary>
        private static uint? FindGprAddress(DebugRegisterSnapshot regs, uint regIndex)
        {
            string[] gprNames = { "zero", "at", "v0", "v1", "a0", "a1", "a2", "a3",
                                  "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
                                  "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
                                  "t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra" };
            if (regIndex >= 32) return null;
            string name = gprNames[regIndex];
            foreach (var cat in regs.Categories)
            {
                foreach (var reg in cat.Registers)
                {
                    if (reg.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        string? raw = reg.Value;
                        if (string.IsNullOrWhiteSpace(raw))
                            return null;
                        // Strip 0x prefix and underscores, take last 8 hex chars (low 32 bits)
                        string text = raw.Trim();
                        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            text = text[2..];
                        text = text.Replace("_", "").Trim();
                        if (text.Length > 8)
                            text = text[^8..];
                        if (uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint addr))
                            return addr;
                        return null;
                    }
                }
            }
            return null;
        }

        private void ApplyMemoryBreakpointInput(bool isRead, bool showErrors)
        {
            CheckBox? chk = isRead ? _chkReadBreakpoint : _chkWriteBreakpoint;
            TextBox? txt = isRead ? _txtReadBreakpoint : _txtWriteBreakpoint;
            if (chk == null || txt == null)
                return;

            uint? oldAddress = isRead ? _readMemcheckAddress : _writeMemcheckAddress;

            if (!chk.Checked)
            {
                txt.BackColor = _themeWindowBack;

                uint? previousReadAddress = _readMemcheckAddress;
                uint? previousWriteAddress = _writeMemcheckAddress;
                long previousReadHits = _readMemcheckHits;
                long previousWriteHits = _writeMemcheckHits;
                bool previousWatchpointsSuspended = _watchpointsSuspended;

                if (isRead)
                {
                    _readMemcheckAddress = null;
                    _readMemcheckHits = -1;
                }
                else
                {
                    _writeMemcheckAddress = null;
                    _writeMemcheckHits = -1;
                }
                _watchpointsSuspended = false;

                try
                {
                    if (oldAddress.HasValue && EnsureDebugServerConnected(forceRetry: false))
                    {
                        SyncDesiredBreakpointsToServer();
                    }
                }
                catch
                {
                    _readMemcheckAddress = previousReadAddress;
                    _writeMemcheckAddress = previousWriteAddress;
                    _readMemcheckHits = previousReadHits;
                    _writeMemcheckHits = previousWriteHits;
                    _watchpointsSuspended = previousWatchpointsSuspended;
                    throw;
                }

                _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(250);
                _disasmList?.Invalidate();
                return;
            }

            if (!TryParseEightHex(txt.Text, out uint address))
            {
                txt.BackColor = _themeEditInvalidBack;
                if (showErrors)
                    MessageBox.Show("Enter an 8-digit hex address.", isRead ? "OnRead" : "OnWrite", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureDebugServerConnected(forceRetry: true))
            {
                txt.BackColor = _themeEditInvalidBack;
                if (showErrors)
                    MessageBox.Show($"Watchpoints require a PCSX2 build exposing the optional debug server on port {_debugServer.Port}.", isRead ? "OnRead" : "OnWrite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                chk.Checked = false;
                return;
            }

            try
            {
                if (!TryBuildWatchpointRange(address, out uint normalizedAddress, out _))
                {
                    txt.BackColor = _themeEditInvalidBack;
                    if (showErrors)
                        MessageBox.Show($"Address 0x{address:X8} is outside EE RAM or would cross the EE RAM boundary.", isRead ? "OnRead" : "OnWrite", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                address = normalizedAddress;

                uint? previousReadAddress = _readMemcheckAddress;
                uint? previousWriteAddress = _writeMemcheckAddress;
                long previousReadHits = _readMemcheckHits;
                long previousWriteHits = _writeMemcheckHits;
                bool previousWatchpointsSuspended = _watchpointsSuspended;

                if (isRead)
                {
                    _readMemcheckAddress = address;
                    _readMemcheckHits = -1;
                }
                else
                {
                    _writeMemcheckAddress = address;
                    _writeMemcheckHits = -1;
                }
                _watchpointsSuspended = false;

                try
                {
                    SyncDesiredBreakpointsToServer();
                }
                catch
                {
                    _readMemcheckAddress = previousReadAddress;
                    _writeMemcheckAddress = previousWriteAddress;
                    _readMemcheckHits = previousReadHits;
                    _writeMemcheckHits = previousWriteHits;
                    _watchpointsSuspended = previousWatchpointsSuspended;
                    throw;
                }

                txt.Text = address.ToString("X8");
                txt.BackColor = _themeEditValidBack;
                _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(250);
                _disasmList?.Invalidate();
            }
            catch (Exception ex)
            {
                txt.BackColor = _themeEditInvalidBack;
                if (showErrors)
                    MessageBox.Show(ex.Message, isRead ? "OnRead" : "OnWrite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Access Monitor ─────────────────────────────────────────────────

        private void StartAccessMonitorOnSelectedRow()
        {
            if (_selRow < 0 || _selRow >= _rows.Count) return;

            var r = ResolveRowForDisplay(_rows[_selRow]);
            uint address = r.Address;

            _accessMonitorType = "readwrite";
            _accessMonitorSizeBytes = 4u;

            if (TryGetAccessMonitorSpec(r, _selRow, out _, out string monitorType, out uint monitorSizeBytes))
            {
                _accessMonitorType = monitorType;
                _accessMonitorSizeBytes = Math.Max(1u, monitorSizeBytes);
            }

            StartAccessMonitor(address);
        }

        private void CenterOwnedWindowOnMainForm(Form child)
        {
            Rectangle ownerBounds = Bounds;
            int x = ownerBounds.Left + Math.Max(0, (ownerBounds.Width - child.Width) / 2);
            int y = ownerBounds.Top + Math.Max(0, (ownerBounds.Height - child.Height) / 2);
            child.StartPosition = FormStartPosition.Manual;
            child.Location = new Point(x, y);
        }

        private void ShowAccessMonitorWindow()
        {
            ShowAccessMonitorForm();
            RefreshAccessMonitorList();
        }

        private string GetAccessMonitorWindowTitle()
        {
            return _accessMonitorAddressValid
                ? $"Access Monitor - {_accessMonitorAddress:X8}"
                : "Access Monitor";
        }

        private void UpdateAccessMonitorWindowState()
        {
            if (_accessMonitorForm != null && !_accessMonitorForm.IsDisposed)
                _accessMonitorForm.Text = GetAccessMonitorWindowTitle();

            if (_accessMonitorStatusLabel != null && !_accessMonitorStatusLabel.IsDisposed)
            {
                _accessMonitorStatusLabel.Text = _accessMonitorActive
                    ? $"Monitoring {_accessMonitorAddress:X8}"
                    : (_accessMonitorAddressValid ? $"Stopped. Last address {_accessMonitorAddress:X8}" : "Not monitoring.");
            }

            if (_accessMonitorGoToAddressButton != null && !_accessMonitorGoToAddressButton.IsDisposed)
                _accessMonitorGoToAddressButton.Enabled = _accessMonitorAddressValid;
        }

        private void GoToAccessMonitorAddress()
        {
            if (!_accessMonitorAddressValid)
                return;

            if (_mainTabs != null)
                _mainTabs.SelectedIndex = 0;

            if (TryGetRowIndexByAddress(_accessMonitorAddress, out int rowIdx))
            {
                SelectRow(rowIdx, center: true);
                _disasmList?.Focus();
                return;
            }

            MessageBox.Show($"Address {_accessMonitorAddress:X8} is outside the loaded disassembly.",
                "Access Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StartAccessMonitor(uint address)
        {
            if (!EnsureDebugServerConnected(forceRetry: true))
            {
                MessageBox.Show($"Access monitor requires a PCSX2 build exposing the debug server on port {_debugServer.Port}.",
                    "Access Monitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Stop any existing monitor
            StopAccessMonitor(quiet: true);

            try
            {
                if (!TryBuildAccessMonitorRange(address, out uint normalizedAddress, out uint endAddress))
                {
                    MessageBox.Show($"Access monitor range 0x{address:X8} + {_accessMonitorSizeBytes} byte(s) is outside EE RAM or crosses the EE RAM boundary.",
                        "Access Monitor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                address = normalizedAddress;

                if (!TryPauseDebugServerForMutation("setting the access monitor", out bool resumeAfterMutation, out string? pauseError))
                    throw new IOException(pauseError ?? "Could not pause PCSX2 before setting the access monitor.");

                try
                {
                    try { _debugServer.RemoveMemcheck(address, endAddress); } catch { }

                    _debugServer.SetMemcheck(address, endAddress, _accessMonitorType,
                        action: "break", description: "ps2dis# AccessMonitor");
                    _accessMonitorMemcheckInstalled = true;
                }
                finally
                {
                    ResumeDebugServerAfterMutation(resumeAfterMutation);
                }

                _accessMonitorAddress = address;
                _accessMonitorAddressValid = true;
                _accessMonitorActive = true;
                _accessMonitorPassiveMode = false;
                _accessMonitorNeedsRearm = false;
                _accessMonitorHits.Clear();
                _accessMonitorPcOrder.Clear();
                _accessMonitorParents.Clear();
                _accessMonitorRows.Clear();
                _accessMonitorLastObservedHits = -1;
                _accessMonitorLastObservedPc = 0;
                _accessMonitorBurstPc = 0;
                _accessMonitorBurstHitCount = 0;
                _accessMonitorPassiveMissingPolls = 0;
                _accessMonitorPassiveFailurePolls = 0;
                _accessMonitorNextUiRefresh = DateTime.UtcNow;
                _accessMonitorNextServerPollUtc = DateTime.UtcNow;
                _accessMonitorRearmUtc = DateTime.MinValue;
                _accessMonitorBurstWindowUtc = DateTime.MinValue;

                _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(20);
                ShowAccessMonitorForm();
                RefreshAccessMonitorList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start access monitor: {ex.Message}",
                    "Access Monitor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopAccessMonitor(bool quiet = false, bool resumeIfPaused = true)
        {
            if (!_accessMonitorActive) return;

            _accessMonitorActive = false;
            _accessMonitorPassiveMode = false;
            _accessMonitorMemcheckInstalled = false;
            _accessMonitorNeedsRearm = false;
            _accessMonitorPassiveMissingPolls = 0;
            _accessMonitorPassiveFailurePolls = 0;
            _accessMonitorBurstPc = 0;
            _accessMonitorBurstHitCount = 0;
            _accessMonitorLastObservedHits = -1;
            _accessMonitorLastObservedPc = 0;
            if (EnsureDebugServerConnected(forceRetry: false))
            {
                bool resumeAfterMutation = false;
                bool pausedForRemoval = TryPauseDebugServerForMutation("stopping the access monitor", out resumeAfterMutation, out _);
                try
                {
                    if (pausedForRemoval)
                        try { _debugServer.RemoveMemcheck(_accessMonitorAddress, GetAccessMonitorEndAddress(_accessMonitorAddress)); } catch { }
                }
                finally
                {
                    if (resumeIfPaused)
                    {
                        // Resume PCSX2 if it was already paused on a memcheck hit, or if we paused it for safe removal.
                        try
                        {
                            var st = _debugServer.GetStatus();
                            if (st.Paused) _debugServer.Resume();
                        }
                        catch { }
                    }
                    else
                    {
                        ResumeDebugServerAfterMutation(resumeAfterMutation);
                    }
                }
            }

            // Final UI refresh to show accumulated counts
            RefreshAccessMonitorList();
        }

        private void ShowAccessMonitorForm()
        {
            if (_accessMonitorForm != null && !_accessMonitorForm.IsDisposed)
            {
                _accessMonitorForm.Text = GetAccessMonitorWindowTitle();
                UpdateAccessMonitorWindowState();
                ApplyThemeToControlTree(_accessMonitorForm);
                ResizeAccessMonitorColumns();
                ApplyThemeToWindowChrome(_accessMonitorForm, forceFrameRefresh: true);
                if (!_accessMonitorForm.Visible)
                {
                    CenterOwnedWindowOnMainForm(_accessMonitorForm);
                    _accessMonitorForm.Show(this);
                }
                _accessMonitorForm.BringToFront();
                return;
            }

            var frm = new Form
            {
                Text = GetAccessMonitorWindowTitle(),
                Width = 550,
                Height = 400,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.Sizable,
                ShowInTaskbar = false,
                MaximizeBox = false,
                MinimizeBox = false,
                MinimumSize = new Size(450, 260),
                BackColor = _themeFormBack,
                ForeColor = _themeFormFore,
            };

            var statusLabel = new Label
            {
                Text = _accessMonitorActive ? $"Monitoring {_accessMonitorAddress:X8}" : (_accessMonitorAddressValid ? $"Stopped. Last address {_accessMonitorAddress:X8}" : "Not monitoring."),
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = _themeFormFore,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0),
            };
            _accessMonitorStatusLabel = statusLabel;

            var lv = new VirtualDisasmList
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                GridLines = false,
                BorderStyle = BorderStyle.None,
                BackColor = _themeWindowBack,
                ForeColor = _themeWindowFore,
                Font = _disasmList?.Font ?? new Font("Consolas", 9f),
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HeaderHeight = Math.Max(20, (_disasmList?.HeaderHeight ?? 20)),
                RowHeight = Math.Max(18, (_disasmList?.RowHeight ?? 18)),
                VirtualMode = true,
                OwnerDraw = true,
                Tag = "AccessMonitorList",
            };
            lv.Columns.Add("Count", 65);
            lv.Columns.Add("Address", 79);
            lv.Columns.Add("Parent", 79);
            lv.Columns.Add("Instruction", 300);
            lv.HeaderBackColor = _headerBack;
            lv.HeaderBorderColor = _headerBorder;
            lv.RetrieveVirtualItem += OnAccessMonitorRetrieveVirtualItem;
            lv.DrawHeader += OnAccessMonitorDrawHeader;
            lv.DrawCell += OnAccessMonitorDrawCell;
            lv.Resize += (_, _) => ResizeAccessMonitorColumns();
            lv.MouseDoubleClick += OnAccessMonitorListDoubleClick;
            _accessMonitorList = lv;

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 286,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(4),
                Margin = new Padding(0),
                BackColor = _themeFormBack,
            };

            var btnStop = new Button { Text = "Stop", Width = 70, FlatStyle = FlatStyle.Flat, ForeColor = _themeFormFore, BackColor = _themeFormBack };
            btnStop.Click += (_, _) => StopAccessMonitor();

            var btnClear = new Button { Text = "Clear", Width = 70, FlatStyle = FlatStyle.Flat, ForeColor = _themeFormFore, BackColor = _themeFormBack };
            btnClear.Click += (_, _) =>
            {
                _accessMonitorHits.Clear();
                _accessMonitorPcOrder.Clear();
                _accessMonitorParents.Clear();
                _accessMonitorRows.Clear();
                _accessMonitorLastObservedHits = -1;
                _accessMonitorLastObservedPc = 0;
                _accessMonitorBurstPc = 0;
                _accessMonitorBurstHitCount = 0;
                RefreshAccessMonitorList();
            };

            var btnGoToAddress = new Button { Text = "Go to Address", Width = 110, FlatStyle = FlatStyle.Flat, ForeColor = _themeFormFore, BackColor = _themeFormBack, Enabled = _accessMonitorAddressValid };
            btnGoToAddress.Click += (_, _) => GoToAccessMonitorAddress();
            _accessMonitorGoToAddressButton = btnGoToAddress;

            btnPanel.Controls.Add(btnStop);
            btnPanel.Controls.Add(btnClear);
            btnPanel.Controls.Add(btnGoToAddress);

            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                Padding = new Padding(0),
                Margin = new Padding(0),
                BackColor = _themeFormBack,
            };
            bottomBar.Controls.Add(statusLabel);
            bottomBar.Controls.Add(btnPanel);

            frm.Controls.Add(lv);
            frm.Controls.Add(bottomBar);

            frm.Shown += (_, _) => ApplyThemeToWindowChrome(frm, forceFrameRefresh: true);
            frm.FormClosing += (_, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    StopAccessMonitor(quiet: true);
                    e.Cancel = true;
                    frm.Hide();
                    return;
                }

                _accessMonitorForm = null;
                _accessMonitorList = null;
                _accessMonitorStatusLabel = null;
                _accessMonitorGoToAddressButton = null;
            };

            _accessMonitorForm = frm;
            UpdateAccessMonitorWindowState();
            ApplyThemeToControlTree(frm);
            CenterOwnedWindowOnMainForm(frm);
            frm.Show(this);
            ResizeAccessMonitorColumns();
            ApplyThemeToWindowChrome(frm, forceFrameRefresh: true);
        }

        private void RefreshAccessMonitorList()
        {
            if (_accessMonitorList == null || _accessMonitorList.IsDisposed)
                return;

            long totalHits = 0;
            foreach (var kv in _accessMonitorHits)
                totalHits += kv.Value;

            int desiredRowCount = _accessMonitorPcOrder.Count;
            bool sizeChanged = desiredRowCount != _accessMonitorRows.Count;
            int firstChanged = int.MaxValue;
            int lastChanged = -1;

            for (int i = 0; i < desiredRowCount; i++)
            {
                uint pc = _accessMonitorPcOrder[i];
                if (!_accessMonitorHits.TryGetValue(pc, out long count))
                    continue;

                string instrText = GetAccessMonitorInstructionText(pc);
                uint parent = _accessMonitorParents.TryGetValue(pc, out uint p) ? p : 0u;

                if (i >= _accessMonitorRows.Count)
                {
                    _accessMonitorRows.Add(new AccessMonitorRow
                    {
                        Pc = pc,
                        Count = count,
                        Parent = parent,
                        Instruction = instrText,
                    });
                    firstChanged = Math.Min(firstChanged, i);
                    lastChanged = i;
                    continue;
                }

                var row = _accessMonitorRows[i];
                if (row.Pc != pc || row.Count != count || row.Parent != parent || !string.Equals(row.Instruction, instrText, StringComparison.Ordinal))
                {
                    row.Pc = pc;
                    row.Count = count;
                    row.Parent = parent;
                    row.Instruction = instrText;
                    firstChanged = Math.Min(firstChanged, i);
                    lastChanged = i;
                }
            }

            while (_accessMonitorRows.Count > desiredRowCount)
            {
                _accessMonitorRows.RemoveAt(_accessMonitorRows.Count - 1);
                sizeChanged = true;
                firstChanged = Math.Min(firstChanged, _accessMonitorRows.Count);
                lastChanged = Math.Max(lastChanged, _accessMonitorRows.Count);
            }

            if (_accessMonitorList.VirtualListSize != _accessMonitorRows.Count)
            {
                _accessMonitorList.VirtualListSize = _accessMonitorRows.Count;
                sizeChanged = true;
            }

            ResizeAccessMonitorColumns();

            if (!sizeChanged && firstChanged <= lastChanged)
                _accessMonitorList.RedrawItems(firstChanged, lastChanged, invalidateOnly: true);
            else if (sizeChanged)
                _accessMonitorList.Invalidate();

            UpdateAccessMonitorWindowState();
        }

        private void SwitchAccessMonitorToBreakMode()
        {
            if (!_accessMonitorActive || !_accessMonitorPassiveMode)
                return;

            try
            {
                if (!TryBuildAccessMonitorRange(_accessMonitorAddress, out uint accessStart, out uint endAddress))
                {
                    _accessMonitorActive = false;
                    _accessMonitorMemcheckInstalled = false;
                    return;
                }

                _accessMonitorAddress = accessStart;
                if (!TryPauseDebugServerForMutation("switching the access monitor to break mode", out bool resumeAfterMutation, out _))
                    return;

                try
                {
                    try
                    {
                        if (_accessMonitorMemcheckInstalled)
                            _debugServer.RemoveMemcheck(_accessMonitorAddress, endAddress);
                    }
                    catch { }

                    _debugServer.SetMemcheck(_accessMonitorAddress, endAddress, _accessMonitorType,
                        action: "break", description: "ps2dis# AccessMonitor");
                    _accessMonitorMemcheckInstalled = true;
                }
                finally
                {
                    ResumeDebugServerAfterMutation(resumeAfterMutation);
                }
                _accessMonitorPassiveMode = false;
                _accessMonitorNeedsRearm = false;
                _accessMonitorPassiveMissingPolls = 0;
                _accessMonitorPassiveFailurePolls = 0;
                _accessMonitorNextServerPollUtc = DateTime.UtcNow;
                _nextDebuggerPollUtc = DateTime.UtcNow.AddMilliseconds(20);
                RefreshAccessMonitorList();
            }
            catch
            {
                _accessMonitorPassiveFailurePolls = 0;
                _accessMonitorPassiveMissingPolls = 0;
            }
        }

        private void RearmAccessMonitorIfNeeded(DebugServerStatus status)
        {
            if (!_accessMonitorActive || _accessMonitorPassiveMode || !_accessMonitorNeedsRearm)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < _accessMonitorRearmUtc || status.Paused)
                return;

            bool pausedForRearm = false;
            bool wasRunning = !status.Paused;
            try
            {
                if (wasRunning)
                {
                    _debugServer.Pause();
                    for (int i = 0; i < 20; i++)
                    {
                        Thread.Sleep(10);
                        try
                        {
                            if (_debugServer.GetStatus().Paused)
                            {
                                pausedForRearm = true;
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }

                    if (!pausedForRearm)
                        throw new IOException("Timed out pausing PCSX2 while rearming the access monitor.");
                }
                else
                {
                    pausedForRearm = true;
                }

                if (!TryBuildAccessMonitorRange(_accessMonitorAddress, out uint accessStart, out uint accessEnd))
                    return;

                _accessMonitorAddress = accessStart;
                _debugServer.SetMemcheck(accessStart, accessEnd, _accessMonitorType,
                    action: "break", description: "ps2dis# AccessMonitor");
                _accessMonitorMemcheckInstalled = true;
                _accessMonitorNeedsRearm = false;
                _nextDebuggerPollUtc = now.AddMilliseconds(12);
                RefreshAccessMonitorList();
            }
            catch
            {
                _accessMonitorRearmUtc = now.AddMilliseconds(120);
            }
            finally
            {
                if (wasRunning && pausedForRearm && _accessMonitorActive)
                {
                    try { _debugServer.Resume(); } catch { }
                }
            }
        }

        private void UpdateAccessMonitorBreakThrottle(uint pc)
        {
            DateTime now = DateTime.UtcNow;
            if (pc == _accessMonitorBurstPc && now <= _accessMonitorBurstWindowUtc)
                _accessMonitorBurstHitCount++;
            else
            {
                _accessMonitorBurstPc = pc;
                _accessMonitorBurstHitCount = 1;
            }

            _accessMonitorBurstWindowUtc = now.AddMilliseconds(160);

            // When the same PC is hitting rapidly, slow down the poll interval to give
            // PCSX2 time to run between hits.  We intentionally keep the memcheck
            // installed so that hits from *other* PCs are never silently lost during
            // a cooldown window.
            int extraDelayMs = 0;
            if (_accessMonitorBurstHitCount >= 6)
                extraDelayMs = 100;
            else if (_accessMonitorBurstHitCount >= 3)
                extraDelayMs = 40;

            if (extraDelayMs > 0)
                _nextDebuggerPollUtc = now.AddMilliseconds(extraDelayMs);
        }

        private void RecordAccessMonitorHit(uint pc, long delta, uint parent = 0)
        {
            if (delta <= 0)
                return;

            pc = NormalizeMipsAddress(pc);
            if (_accessMonitorHits.TryGetValue(pc, out long count))
            {
                _accessMonitorHits[pc] = count + delta;
            }
            else
            {
                _accessMonitorHits[pc] = delta;
                _accessMonitorPcOrder.Add(pc);
            }

            if (parent != 0 && !_accessMonitorParents.ContainsKey(pc))
                _accessMonitorParents[pc] = NormalizeMipsAddress(parent);
        }

        private void PollAccessMonitorMemcheck()
        {
            if (!_accessMonitorActive || !_accessMonitorPassiveMode)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < _accessMonitorNextServerPollUtc)
                return;

            _accessMonitorNextServerPollUtc = now.AddMilliseconds(75);

            try
            {
                var memchecks = _debugServer.ListMemchecks();
                var info = FindTrackedMemcheck(memchecks, _accessMonitorAddress);
                if (info == null)
                {
                    _accessMonitorPassiveMissingPolls++;
                    if (_accessMonitorPassiveMissingPolls >= 2)
                        SwitchAccessMonitorToBreakMode();
                    return;
                }

                _accessMonitorPassiveMissingPolls = 0;
                _accessMonitorPassiveFailurePolls = 0;

                long currentHits = Math.Max(0, info.Hits);
                uint normalizedLastPc = NormalizeMipsAddress(info.LastPc);
                if (_accessMonitorLastObservedHits < 0)
                {
                    if (currentHits > 0)
                    {
                        uint bootstrapPc = normalizedLastPc != 0 ? normalizedLastPc : _accessMonitorLastObservedPc;
                        if (bootstrapPc != 0)
                            RecordAccessMonitorHit(bootstrapPc, currentHits);
                    }
                    _accessMonitorLastObservedHits = currentHits;
                    _accessMonitorLastObservedPc = normalizedLastPc;
                    return;
                }

                long delta = currentHits - _accessMonitorLastObservedHits;
                if (delta > 0)
                {
                    uint samplePc = normalizedLastPc != 0 ? normalizedLastPc : _accessMonitorLastObservedPc;
                    if (samplePc != 0)
                        RecordAccessMonitorHit(samplePc, delta);
                }

                if (normalizedLastPc != 0)
                    _accessMonitorLastObservedPc = normalizedLastPc;

                _accessMonitorLastObservedHits = currentHits;

                if (now >= _accessMonitorNextUiRefresh)
                {
                    RefreshAccessMonitorList();
                    _accessMonitorNextUiRefresh = now.AddMilliseconds(150);
                }
            }
            catch
            {
                _accessMonitorPassiveFailurePolls++;
                if (_accessMonitorPassiveFailurePolls >= 2)
                    SwitchAccessMonitorToBreakMode();
                else
                    _accessMonitorNextServerPollUtc = now.AddMilliseconds(250);
            }
        }

        private string GetAccessMonitorInstructionText(uint pc)
        {
            if (TryGetRowIndexByAddress(pc, out int rowIdx))
            {
                var row = GetLiveDisplayRow(rowIdx);
                return GetCommandText(row, rowIdx).Replace(AnnotationSentinel.ToString(), string.Empty);
            }

            return "(outside loaded range)";
        }

        private void ResizeAccessMonitorColumns()
        {
            if (_accessMonitorList == null || _accessMonitorList.Columns.Count < 4)
                return;

            int countWidth = _accessMonitorList.Columns[0].Width;
            int addressWidth = _accessMonitorList.Columns[1].Width;
            int parentWidth = _accessMonitorList.Columns[2].Width;
            int scrollbarW = _accessMonitorRows.Count > _accessMonitorList.VisibleRowCapacity ? SystemInformation.VerticalScrollBarWidth : 0;
            int remaining = Math.Max(120, _accessMonitorList.ClientSize.Width - countWidth - addressWidth - parentWidth - scrollbarW - 2);
            _accessMonitorList.Columns[3].Width = remaining;
        }

        private void OnAccessMonitorRetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _accessMonitorRows.Count)
            {
                e.Item = new ListViewItem(string.Empty);
                return;
            }

            var row = _accessMonitorRows[e.ItemIndex];
            var item = new ListViewItem(row.Count.ToString("N0"));
            item.SubItems.Add(row.Pc.ToString("X8"));
            item.SubItems.Add(row.Parent != 0 ? row.Parent.ToString("X8") : "");
            item.SubItems.Add(row.Instruction);
            EnsureVirtualItemHasAllSubItems(_accessMonitorList!, item);
            e.Item = item;
        }

        private void OnAccessMonitorDrawHeader(object? sender, VirtualDisasmList.VirtualHeaderPaintEventArgs e)
        {
            using var back = new SolidBrush(_headerBack);
            e.Graphics.FillRectangle(back, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, _accessMonitorList?.Font ?? Font,
                Rectangle.Inflate(e.Bounds, -4, 0), _headerFore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            using var pen = new Pen(_headerBorder);
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private void OnAccessMonitorDrawCell(object? sender, VirtualDisasmList.VirtualCellPaintEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _accessMonitorRows.Count || _accessMonitorList == null)
                return;

            var row = _accessMonitorRows[e.ItemIndex];
            bool dark = _accessMonitorList.BackColor.GetBrightness() < 0.45f;
            Color back = e.Selected
                ? (dark ? Color.FromArgb(58, 74, 98) : Color.FromArgb(0, 0, 128))
                : _accessMonitorList.BackColor;
            Color fore = e.Selected ? Color.White : _accessMonitorList.ForeColor;

            using var b = new SolidBrush(back);
            e.Graphics.FillRectangle(b, e.Bounds);

            // Column order: 0=Count, 1=Address, 2=Parent, 3=Instruction
            string text = e.ColumnIndex switch
            {
                0 => row.Count.ToString("N0"),
                1 => row.Pc.ToString("X8"),
                2 => row.Parent != 0 ? row.Parent.ToString("X8") : "",
                _ => row.Instruction,
            };

            var textBounds = Rectangle.Inflate(e.Bounds, -4, 0);
            if (e.ColumnIndex == 3)
            {
                DrawFormattedCommandText(e.Graphics, textBounds, text, e.Selected,
                    defaultColor: ColAddr,
                    selectedTextColor: Color.White,
                    annotationColor: e.Selected ? Color.White : (_currentTheme == AppTheme.Dark ? Color.FromArgb(128, 128, 128) : Color.Gray));
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, text, _accessMonitorList.Font,
                    textBounds, fore,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        private void OnAccessMonitorListDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_accessMonitorList == null)
                return;

            var hit = _accessMonitorList.HitTest(new Point(e.X, e.Y));
            if (hit?.Item == null)
                return;

            int index = hit.Item.Index;
            if (index < 0 || index >= _accessMonitorRows.Count)
                return;

            uint target = 0;
            // Column 1 = Address, Column 2 = Parent
            if (hit.ColumnIndex == 1)
                target = _accessMonitorRows[index].Pc;
            else if (hit.ColumnIndex == 2)
                target = _accessMonitorRows[index].Parent;
            else
                target = _accessMonitorRows[index].Pc;

            if (target == 0)
                return;

            if (_mainTabs != null)
                _mainTabs.SelectedIndex = 0;
            if (TryGetRowIndexByAddress(target, out int rowIdx))
            {
                SelectRow(rowIdx, center: true);
                _disasmList?.Focus();
            }
        }

        private void PopulateCallStack(IReadOnlyList<DebugBacktraceFrame> frames, DebugRegisterSnapshot? regs = null)
        {
            if (_txtCallStack == null)
                return;

            uint breakAddress = _activeBreakpointAddress ?? _lastDebuggerPc;
            var lines = new List<string> { $"Break @ {breakAddress:X8}" };
            foreach (DebugBacktraceFrame frame in frames)
            {
                uint addr = frame.Pc != 0 ? frame.Pc : frame.Entry;
                if (addr == 0 || addr == breakAddress)
                    continue;
                // Skip sentinel FFFFFFFF frames — we show thread info separately below
                if (addr == 0xFFFFFFFF || addr == 0x7FFFFFFF)
                    continue;
                lines.Add(addr.ToString("X8"));
            }

            // Always append current thread ID at the bottom
            int threadId = TryReadCurrentEeThreadId(frames, regs);
            lines.Add(threadId >= 0 ? $"Thread {threadId}" : "Thread ?");

            _txtCallStack.Text = string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Best-effort read of the current EE thread ID from PS2 kernel RAM.
        /// The BIOS stores the running thread's ID at 0x800125EC and thread IDs are 1-based,
        /// so prefer that canonical word first. Only fall back to a TCB/stack match when the
        /// current-thread word is unavailable on the user's PCSX2 build.
        /// </summary>
        private int TryReadCurrentEeThreadId(IReadOnlyList<DebugBacktraceFrame> frames, DebugRegisterSnapshot? regs = null)
        {
            int debugServerThreadId = TryReadCurrentEeThreadIdFromDebugServer(frames, regs);
            if (debugServerThreadId >= 0)
                return debugServerThreadId;

            int currentThreadId = TryReadCanonicalEeThreadId();
            if (currentThreadId >= 0)
                return currentThreadId;

            if (TryGetCurrentEeStackPointer(frames, regs, out uint currentSp))
            {
                int stackThreadId = TryResolveEeThreadIdFromStackPointer(currentSp);
                if (stackThreadId >= 0)
                    return stackThreadId;
            }

            return -1;
        }

        private int TryReadCurrentEeThreadIdFromDebugServer(IReadOnlyList<DebugBacktraceFrame> frames, DebugRegisterSnapshot? regs)
        {
            if (!_debugServerAvailable || !_debugServer.IsConnected)
                return -1;

            try
            {
                IReadOnlyList<DebugThreadInfo> threads = _debugServer.ListThreads();
                if (threads == null || threads.Count == 0)
                    return -1;

                uint breakpointPc = NormalizeMipsAddress(_activeBreakpointAddress
                    ?? _pausedBreakpointUiAddress
                    ?? _breakpointUiFrozenAddress
                    ?? _lastDebuggerPc);

                var exactCandidates = new HashSet<uint>();
                var nearbyCandidates = new HashSet<uint>();

                void AddPcCandidate(uint address)
                {
                    address = NormalizeMipsAddress(address);
                    if (address == 0)
                        return;

                    exactCandidates.Add(address);
                    if (address >= 4)
                        nearbyCandidates.Add(address - 4);
                    nearbyCandidates.Add(address + 4);
                }

                AddPcCandidate(breakpointPc);

                if (TryGetGeneralRegisterValue(regs, "pc", out uint regsPc))
                    AddPcCandidate(regsPc);

                if (frames != null)
                {
                    foreach (DebugBacktraceFrame frame in frames)
                    {
                        AddPcCandidate(frame.Pc);
                        AddPcCandidate(frame.Entry);
                    }
                }

                int bestThreadId = -1;
                int bestScore = int.MinValue;
                int runningThreadId = -1;
                int runningThreadCount = 0;
                foreach (DebugThreadInfo thread in threads)
                {
                    if (thread.Id < 0 || thread.Id >= EeMaxThreadCount)
                        continue;

                    uint threadPc = NormalizeMipsAddress(thread.Pc);
                    int score = 0;

                    if (thread.Status == 0x01)
                    {
                        runningThreadId = thread.Id;
                        runningThreadCount++;
                    }

                    if (threadPc != 0 && exactCandidates.Contains(threadPc))
                        score += 100;
                    else if (threadPc != 0 && nearbyCandidates.Contains(threadPc))
                        score += 70;

                    if (threadPc != 0 && breakpointPc != 0)
                    {
                        uint delta = threadPc >= breakpointPc ? threadPc - breakpointPc : breakpointPc - threadPc;
                        if (delta <= 0x10)
                            score += 20;
                    }

                    if (thread.Status == 0x01)
                        score += 25;
                    else if (thread.Status == 0x02)
                        score += 5;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestThreadId = thread.Id;
                    }
                }

                if (bestScore >= 70)
                    return bestThreadId;

                if (runningThreadCount == 1 && runningThreadId >= 0)
                    return runningThreadId;

                if (bestThreadId >= 0 && bestScore > 0)
                    return bestThreadId;

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private int TryReadCanonicalEeThreadId()
        {
            uint normalizedAddress = NormalizeMipsAddress(EeCurrentThreadIdAddress);

            if (_pineAvailable && _pine.IsConnected)
            {
                try
                {
                    byte[]? pineData = _pine.ReadMemory(normalizedAddress, 4);
                    if (pineData != null && pineData.Length >= 4)
                    {
                        uint rawThreadId = BitConverter.ToUInt32(pineData, 0);
                        if (rawThreadId < EeMaxThreadCount)
                            return (int)rawThreadId;
                    }
                }
                catch { }
            }

            if (TryReadEeMemoryDirect(normalizedAddress, 4, out byte[] directData) && directData.Length >= 4)
            {
                uint rawThreadId = BitConverter.ToUInt32(directData, 0);
                if (rawThreadId < EeMaxThreadCount)
                    return (int)rawThreadId;
            }

            return -1;
        }

        private bool TryGetCurrentEeStackPointer(IReadOnlyList<DebugBacktraceFrame> frames, DebugRegisterSnapshot? regs, out uint stackPointer)
        {
            if (TryGetGeneralRegisterValue(regs, "sp", out stackPointer))
                return true;

            if (frames != null)
            {
                foreach (DebugBacktraceFrame frame in frames)
                {
                    if (frame.Sp != 0)
                    {
                        stackPointer = NormalizeMipsAddress(frame.Sp);
                        return true;
                    }
                }
            }

            stackPointer = 0;
            return false;
        }

        private bool TryGetGeneralRegisterValue(DebugRegisterSnapshot? regs, string registerName, out uint value)
        {
            value = 0;
            if (regs == null || string.IsNullOrWhiteSpace(registerName))
                return false;

            string wanted = NormalizeGeneralRegisterName(registerName);
            if (string.IsNullOrWhiteSpace(wanted))
                return false;

            foreach (DebugRegisterCategory category in regs.Categories)
            {
                if (!IsGprCategory(category))
                    continue;

                foreach (DebugRegisterValue reg in category.Registers)
                {
                    if (!string.Equals(NormalizeGeneralRegisterName(reg.Name), wanted, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string raw = string.IsNullOrWhiteSpace(reg.Value) ? (reg.Display ?? string.Empty) : reg.Value!;
                    if (TryParseRegisterUInt32(raw, out value))
                    {
                        value = NormalizeMipsAddress(value);
                        return true;
                    }
                }
            }

            if (string.Equals(wanted, "pc", StringComparison.OrdinalIgnoreCase) && regs.Pc != 0)
            {
                value = NormalizeMipsAddress(regs.Pc);
                return true;
            }

            return false;
        }

        private static bool TryParseRegisterUInt32(string? text, out uint value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string cleaned = text.Trim().Replace("_", string.Empty);
            if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[2..];

            if (cleaned.Length == 0)
                return false;

            if (cleaned.Length > 8)
                cleaned = cleaned[^8..];

            return uint.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private int TryResolveEeThreadIdFromStackPointer(uint currentSp)
        {
            int bestRunningThreadId = -1;
            int bestRunningScore = int.MinValue;
            int bestAnyThreadId = -1;
            int bestAnyScore = int.MinValue;

            for (int threadId = 0; threadId < EeMaxThreadCount; threadId++)
            {
                uint tcbAddress = EeThreadControlBlockBaseAddress + (uint)(threadId * EeThreadControlBlockSize);
                if (!TryReadEeMemory(tcbAddress, EeThreadControlBlockSize, out byte[] tcb) || tcb.Length < EeThreadControlBlockSize)
                    continue;

                uint status = BitConverter.ToUInt32(tcb, 0x08);
                if (!IsPlausibleEeThreadStatus(status))
                    continue;

                uint currentStack = NormalizeMipsAddress(BitConverter.ToUInt32(tcb, 0x10));
                uint initialStack = NormalizeMipsAddress(BitConverter.ToUInt32(tcb, 0x3C));
                uint stackSize = BitConverter.ToUInt32(tcb, 0x40);
                short currentPriority = BitConverter.ToInt16(tcb, 0x18);
                short initialPriority = BitConverter.ToInt16(tcb, 0x1A);

                int score = ScoreEeThreadStackCandidate(currentSp, currentStack, initialStack, stackSize, currentPriority, initialPriority, status);
                if (score < 0)
                    continue;

                if (status == 0x01)
                {
                    if (score > bestRunningScore)
                    {
                        bestRunningScore = score;
                        bestRunningThreadId = threadId;
                    }
                }

                if (score > bestAnyScore)
                {
                    bestAnyScore = score;
                    bestAnyThreadId = threadId;
                }
            }

            if (bestRunningScore >= 0)
                return bestRunningThreadId;

            return bestAnyScore >= 16 ? bestAnyThreadId : -1;
        }

        private static bool IsPlausibleEeThreadStatus(uint status)
        {
            return status == 0x01u || status == 0x02u || status == 0x04u || status == 0x08u || status == 0x0Cu || status == 0x10u;
        }

        private static int ScoreEeThreadStackCandidate(uint currentSp, uint currentStack, uint initialStack, uint stackSize, int currentPriority, int initialPriority, uint status)
        {
            if (currentSp == 0 || stackSize == 0 || stackSize > 0x01000000u)
                return -1;

            if (currentPriority < 0 || currentPriority >= 128 || initialPriority < 0 || initialPriority >= 128)
                return -1;

            int score = -1;

            if (initialStack != 0)
            {
                ulong low = initialStack > stackSize ? initialStack - stackSize : 0;
                ulong highInclusive = initialStack + 0x100u;
                if (currentSp >= low && currentSp <= highInclusive)
                    score = Math.Max(score, 16);
            }

            if (currentStack != 0)
            {
                ulong delta = currentSp >= currentStack ? (ulong)(currentSp - currentStack) : (ulong)(currentStack - currentSp);
                if (delta <= 0x80u)
                    score = Math.Max(score, 28);
                else if (delta <= 0x400u)
                    score = Math.Max(score, 24);
                else if (delta <= 0x1000u)
                    score = Math.Max(score, 18);
            }

            if (score < 0)
                return -1;

            if (status == 0x01u)
                score += 24;
            else if (status == 0x02u)
                score += 4;

            if (currentPriority == initialPriority)
                score += 1;

            return score;
        }

        private bool TryReadEeMemory(uint address, int length, out byte[] data)
        {
            data = Array.Empty<byte>();
            if (length <= 0)
                return false;

            if (!TryBuildEeRamRange(address, (uint)length, out uint normalizedAddress, out _))
                return false;

            bool preferDirectRead = normalizedAddress < 0x00100000u;

            if (preferDirectRead && TryReadEeMemoryDirect(normalizedAddress, length, out data))
                return true;

            if (_pineAvailable && _pine.IsConnected)
            {
                try
                {
                    byte[]? pineData = _pine.ReadMemory(normalizedAddress, length);
                    if (pineData != null && pineData.Length >= length)
                    {
                        if (!preferDirectRead || !IsAllZero(pineData))
                        {
                            data = pineData;
                            return true;
                        }
                    }
                }
                catch { }
            }

            if (!preferDirectRead && TryReadEeMemoryDirect(normalizedAddress, length, out data))
                return true;

            return false;
        }

        private bool TryReadEeMemoryDirect(uint normalizedAddress, int length, out byte[] data)
        {
            data = Array.Empty<byte>();
            if (!TryBuildEeRamRange(normalizedAddress, (uint)Math.Max(0, length), out normalizedAddress, out _))
                return false;
            if (_liveProcId == 0 || _eeHostAddr == 0)
                return false;

            IntPtr hProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_VM_READ, false, _liveProcId);
            if (hProc == IntPtr.Zero)
                return false;

            try
            {
                byte[] buffer = new byte[length];
                if (NativeMethods.ReadProcessMemory(hProc, (IntPtr)(_eeHostAddr + normalizedAddress), buffer, length, out int bytesRead) && bytesRead == length)
                {
                    data = buffer;
                    return true;
                }
            }
            catch { }
            finally
            {
                NativeMethods.CloseHandle(hProc);
            }

            return false;
        }

        private static bool IsAllZero(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != 0)
                    return false;
            }

            return true;
        }

        private void PopulateRegisters(DebugRegisterSnapshot regs)
        {
            if (_fprList == null)
                return;

            _fprRows.Clear();

            foreach (var reg in EnumerateOrderedGeneralRegisters(regs))
                _fprRows.Add((reg.Name, FormatGeneralBreakpointRegisterValue(reg.Value), false));

            var fprs = new Dictionary<int, string>();
            foreach (DebugRegisterCategory category in regs.Categories)
            {
                if (!IsFprCategory(category))
                    continue;

                foreach (DebugRegisterValue reg in category.Registers)
                {
                    if (TryParseFprIndex(reg.Name, out int fprIndex))
                    {
                        string value = string.IsNullOrWhiteSpace(reg.Display)
                            ? (reg.Value ?? string.Empty)
                            : reg.Display!;
                        fprs[fprIndex] = value;
                    }
                }
            }

            for (int i = 0; i <= 31; i++)
            {
                if (!fprs.TryGetValue(i, out string? value))
                    value = string.Empty;
                _fprRows.Add(($"f{i}", value, true));
            }

            UpdateRegisterListColumns(_fprList);
            _fprList.VirtualListSize = _fprRows.Count;
            _fprList.Invalidate();
        }

        private static string FormatGeneralBreakpointRegisterValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string text = value.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text[2..];
            text = text.Replace("_", string.Empty).Trim();
            if (text.Length == 0)
                return string.Empty;

            text = text.ToUpperInvariant();
            if (text.Length > 8)
                text = text[^8..];
            return text.PadLeft(8, '0');
        }

        private static bool TryParseFprIndex(string? name, out int index)
        {
            index = -1;
            if (string.IsNullOrWhiteSpace(name))
                return false;
            string trimmed = name.Trim();
            if (trimmed.Length < 2 || (trimmed[0] != 'f' && trimmed[0] != 'F'))
                return false;
            if (!int.TryParse(trimmed[1..], out index))
                return false;
            return index >= 0 && index <= 31;
        }

        private IEnumerable<(string Name, string Value)> EnumerateOrderedGeneralRegisters(DebugRegisterSnapshot regs)
        {
            var ordered = new List<(string Name, string Value)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddRegister(string name, string? value)
            {
                string displayName = NormalizeGeneralRegisterName(name);
                if (string.IsNullOrWhiteSpace(displayName) || !WantedBreakpointRegister(displayName) || !seen.Add(displayName))
                    return;
                ordered.Add((displayName, value ?? string.Empty));
            }

            foreach (DebugRegisterCategory category in regs.Categories)
            {
                if (!IsGprCategory(category))
                    continue;

                foreach (DebugRegisterValue reg in category.Registers)
                    AddRegister(reg.Name, reg.Value ?? reg.Display);
            }

            AddRegister("pc", regs.Pc.ToString("X8"));
            AddRegister("hi", regs.Hi);
            AddRegister("lo", regs.Lo);

            return ordered.OrderBy(r => GetRegisterSortKey(r.Name)).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
        }

        private static bool WantedBreakpointRegister(string? name)
        {
            string reg = (name ?? string.Empty).Trim().ToLowerInvariant();
            return reg switch
            {
                "zero" or "at" or "v0" or "v1" or
                "a0" or "a1" or "a2" or "a3" or
                "t0" or "t1" or "t2" or "t3" or "t4" or "t5" or "t6" or "t7" or "t8" or "t9" or
                "s0" or "s1" or "s2" or "s3" or "s4" or "s5" or "s6" or "s7" or
                "k0" or "k1" or "gp" or "sp" or "fp" or "ra" or
                "hi" or "low" or "pc" => true,
                _ => false,
            };
        }

        private static string NormalizeGeneralRegisterName(string? name)
        {
            string reg = (name ?? string.Empty).Trim().ToLowerInvariant();
            return reg switch
            {
                "r0" => "zero",
                "r1" => "at",
                "r2" => "v0",
                "r3" => "v1",
                "r4" => "a0",
                "r5" => "a1",
                "r6" => "a2",
                "r7" => "a3",
                "r8" => "t0",
                "r9" => "t1",
                "r10" => "t2",
                "r11" => "t3",
                "r12" => "t4",
                "r13" => "t5",
                "r14" => "t6",
                "r15" => "t7",
                "r16" => "s0",
                "r17" => "s1",
                "r18" => "s2",
                "r19" => "s3",
                "r20" => "s4",
                "r21" => "s5",
                "r22" => "s6",
                "r23" => "s7",
                "r24" => "t8",
                "r25" => "t9",
                "r26" => "k0",
                "r27" => "k1",
                "r28" => "gp",
                "r29" => "sp",
                "r30" => "fp",
                "r31" => "ra",
                "lo" => "low",
                _ => string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim(),
            };
        }

        private static int GetRegisterSortKey(string? name)
        {
            string reg = (name ?? string.Empty).Trim().ToLowerInvariant();
            return reg switch
            {
                "zero" => 0,
                "at" => 1,
                "v0" => 2,
                "v1" => 3,
                "a0" => 4,
                "a1" => 5,
                "a2" => 6,
                "a3" => 7,
                "t0" => 8,
                "t1" => 9,
                "t2" => 10,
                "t3" => 11,
                "t4" => 12,
                "t5" => 13,
                "t6" => 14,
                "t7" => 15,
                "s0" => 16,
                "s1" => 17,
                "s2" => 18,
                "s3" => 19,
                "s4" => 20,
                "s5" => 21,
                "s6" => 22,
                "s7" => 23,
                "t8" => 24,
                "t9" => 25,
                "k0" => 26,
                "k1" => 27,
                "gp" => 28,
                "sp" => 29,
                "fp" => 30,
                "s8" => 30,
                "ra" => 31,
                "hi" => 32,
                "low" => 33,
                "lo" => 33,
                "pc" => 34,
                _ when reg.StartsWith("r") && int.TryParse(reg[1..], out int gprIndex) => 100 + gprIndex,
                _ => 1000,
            };
        }

        private void OnRegisterListRetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _fprRows.Count)
            {
                e.Item = new ListViewItem(string.Empty);
                return;
            }

            var row = _fprRows[e.ItemIndex];
            var item = new ListViewItem(row.Reg);
            item.SubItems.Add(row.Value);
            EnsureVirtualItemHasAllSubItems(_fprList!, item);
            e.Item = item;
        }

        private void OnRegisterListDrawHeader(object? sender, VirtualDisasmList.VirtualHeaderPaintEventArgs e)
        {
            using var b = new SolidBrush(_headerBack);
            e.Graphics.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, _mono, Rectangle.Inflate(e.Bounds, -4, 0), _headerFore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private void OnRegisterListDrawCell(object? sender, VirtualDisasmList.VirtualCellPaintEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _fprRows.Count)
                return;

            var row = _fprRows[e.ItemIndex];
            bool dark = _currentTheme == AppTheme.Dark;
            Color regListBack = dark ? ColBg : _themeWindowBack;
            Color back = e.Selected ? ColSel : regListBack;
            Color regColor = GetRegisterColor(row.Reg) ?? ColFpu;
            if (!dark) regColor = ScaleColor(regColor, 0.25f);
            Color fore = e.Selected ? ColSelFg : (e.ColumnIndex == 0 ? regColor : _themeWindowFore);
            using var b = new SolidBrush(back);
            e.Graphics.FillRectangle(b, e.Bounds);
            string text = e.ColumnIndex == 0 ? row.Reg : row.Value;
            TextRenderer.DrawText(e.Graphics, text, _mono, Rectangle.Inflate(e.Bounds, -4, 0), fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private void OnCallStackMouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (_txtCallStack == null)
                return;

            int charIndex = _txtCallStack.GetCharIndexFromPosition(e.Location);
            if (charIndex < 0)
                return;

            int lineIndex = _txtCallStack.GetLineFromCharIndex(charIndex);
            string[] lines = _txtCallStack.Lines;
            if (lineIndex < 0 || lineIndex >= lines.Length)
                return;

            string line = lines[lineIndex].Trim();
            if (line.StartsWith("Break @ ", StringComparison.OrdinalIgnoreCase))
                line = line[8..].Trim();

            int cut = line.IndexOfAny(new[] { ' ', '	', '#' });
            if (cut >= 0)
                line = line[..cut].Trim();

            if (TryParseEightHex(line, out uint address))
                FocusDebuggerAddress(address);
        }

        private void ApplyPausedBreakpointMenuStatus(uint address)
        {
            if (_menuPauseStatusActive)
            {
                _menuStatusLabel.Text = $"PAUSED: BREAKPOINT at {address:X8}";
                _menuBar.Invalidate();
                return;
            }

            _menuPauseStatusSavedText = _menuStatusLabel.Text;
            _menuPauseStatusSavedColor = _menuStatusLabel.ForeColor;
            _menuPauseStatusSavedFont = _menuStatusLabel.Font;
            _menuPauseStatusActive = true;
            _activityStatusResetTimer.Stop();
            _menuStatusLabel.Text = $"PAUSED: BREAKPOINT at {address:X8}";
            _menuStatusLabel.ForeColor = ColJump;
            if (_menuStatusBoldFont != null)
                _menuStatusLabel.Font = _menuStatusBoldFont;
            UpdateMenuStatusLayout();
            _menuBar.Invalidate();
        }

        private void ClearPausedBreakpointMenuStatus()
        {
            if (!_menuPauseStatusActive)
                return;

            _menuPauseStatusActive = false;
            _menuStatusLabel.ForeColor = _menuPauseStatusSavedColor;
            _menuStatusLabel.Font = _menuPauseStatusSavedFont ?? _menuBar.Font;
            _menuStatusLabel.Text = string.IsNullOrWhiteSpace(_menuPauseStatusSavedText) ? "Ready" : _menuPauseStatusSavedText;
            UpdateMenuStatusLayout();
            _menuBar.Invalidate();
        }

        private void LatchPausedBreakpointUiState(uint address, bool isWatchpoint)
        {
            _pausedBreakpointUiLatched = true;
            _pausedBreakpointUiAddress = address;
            _pausedBreakpointUiIsWatchpoint = isWatchpoint;
            _pausedBreakpointUiRunningPolls = 0;
        }

        private void FreezePausedBreakpointUi(uint address, bool isWatchpoint)
        {
            _breakpointUiFrozen = true;
            _breakpointUiFrozenAddress = address;
            _breakpointUiFrozenIsWatchpoint = isWatchpoint;
            _activeBreakpointAddress = address;
            _activeBreakpointIsWatchpoint = isWatchpoint;
            _lastDebuggerPaused = true;
        }

        private void ClearFrozenBreakpointUi()
        {
            _breakpointUiFrozen = false;
            _breakpointUiFrozenAddress = null;
            _breakpointUiFrozenIsWatchpoint = false;
        }

        private void ClearPausedBreakpointUiState()
        {
            _pausedBreakpointUiLatched = false;
            _pausedBreakpointUiAddress = null;
            _pausedBreakpointUiIsWatchpoint = false;
            _pausedBreakpointUiRunningPolls = 0;
            ClearFrozenBreakpointUi();
            _activeBreakpointAddress = null;
            _activeBreakpointIsWatchpoint = false;
            _lastDebuggerPaused = false;
            // Drop the cached register snapshot so the live break-time
            // annotations on rows around the active break PC are not displayed
            // after the VM resumes.
            _breakRegisterSnapshot = null;
        }

        // ──────────────────────────────────────────────────────────────────
        // Live break-time annotations (visible rows around active break)
        // ──────────────────────────────────────────────────────────────────
        //
        // While paused at a breakpoint, visible disassembler rows can get
        // live annotations backed by the cached register snapshot. Each row is
        // checked for control-flow barriers and intervening register writes so
        // values are only shown when the break-time state still makes sense
        // for that instruction.

        /// <summary>
        /// Returns true when a live register snapshot is available (the VM is
        /// currently paused at a ps2dis#-tracked break) and <paramref name="rowIdx"/>
        /// is currently visible in the disassembler. Individual annotation builders
        /// still perform flow/data-dependency checks before using break-time values.
        /// </summary>
        internal bool IsBreakAnnotationRow(int rowIdx)
        {
            if (!TryGetActiveBreakpointRowIndex(out _))
                return false;

            if (rowIdx < 0 || rowIdx >= _rows.Count)
                return false;

            int top = Math.Max(0, Math.Min(_disasmList.TopIndex, Math.Max(0, _rows.Count - 1)));
            int visible = Math.Max(1, _disasmList.VisibleRowCapacity);
            int bottom = Math.Min(_rows.Count - 1, top + visible); // include a partially visible trailing row
            return rowIdx >= top && rowIdx <= bottom;
        }

        internal bool TryGetActiveBreakpointRowIndex(out int activeIdx)
        {
            activeIdx = -1;
            if (_breakRegisterSnapshot == null || !_activeBreakpointAddress.HasValue)
                return false;

            uint activeAddr = NormalizeMipsAddress(_activeBreakpointAddress.Value);
            return TryGetRowIndexByAddress(activeAddr, out activeIdx);
        }

        internal bool TryComputeBreakDataAddressForRow(uint instructionWord, int rowIdx, out uint address)
        {
            address = 0;
            if (!TryGetActiveBreakpointRowIndex(out int activeIdx))
                return false;

            uint op = (instructionWord >> 26) & 0x3F;
            uint rs = (instructionWord >> 21) & 0x1F;
            uint ui = instructionWord & 0xFFFF;

            if (op == 0x0F)
            {
                address = ui << 16;
                return true;
            }

            bool isLoadStore = IsLoadStore(op);
            bool isAddressBuilder = op is 0x08 or 0x09 or 0x0D;
            if (!isLoadStore && !isAddressBuilder)
                return false;

            if (!CanUseBreakSourceGprForRow(rowIdx, activeIdx, rs))
                return false;

            address = ComputeBreakDataAddress(instructionWord);
            return address != 0;
        }

        internal bool TryGetBreakDestinationValueForRow(int rowIdx, uint instructionWord, out uint destReg, out uint value)
        {
            destReg = 0;
            value = 0;
            if (!TryGetActiveBreakpointRowIndex(out int activeIdx) || rowIdx >= activeIdx)
                return false;

            destReg = GetDestinationGpr(instructionWord);
            if (destReg == 0 || !CanUsePastBreakRow(rowIdx, activeIdx))
                return false;

            for (int i = rowIdx + 1; i < activeIdx; i++)
            {
                if (WritesGprForBreakSnapshot(_rows[i].Word, destReg))
                    return false;
            }

            return TryGetBreakGpr(destReg, out value);
        }

        internal bool HasBreakMemoryWriteBetween(int startInclusive, int endExclusive)
        {
            startInclusive = Math.Max(0, startInclusive);
            endExclusive = Math.Min(_rows.Count, endExclusive);
            for (int i = startInclusive; i < endExclusive; i++)
            {
                uint op = (_rows[i].Word >> 26) & 0x3F;
                if (TryGetLoadStoreAccessSpec(op, out string accessType, out _) &&
                    string.Equals(accessType, "write", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanUseBreakSourceGprForRow(int rowIdx, int activeIdx, uint reg)
        {
            if (rowIdx < 0 || rowIdx >= _rows.Count)
                return false;

            if (rowIdx == activeIdx || reg == 0)
                return true;

            if (rowIdx < activeIdx)
            {
                if (!CanUsePastBreakRow(rowIdx, activeIdx))
                    return false;

                for (int i = rowIdx; i < activeIdx; i++)
                {
                    if (WritesGprForBreakSnapshot(_rows[i].Word, reg))
                        return false;
                }
                return true;
            }

            if (!CanUseFutureBreakRow(rowIdx, activeIdx))
                return false;

            for (int i = activeIdx; i < rowIdx; i++)
            {
                if (WritesGprForBreakSnapshot(_rows[i].Word, reg))
                    return false;
            }
            return true;
        }

        private bool CanUsePastBreakRow(int rowIdx, int activeIdx)
        {
            if (rowIdx < 0 || activeIdx < 0 || rowIdx > activeIdx)
                return false;
            if (rowIdx == activeIdx)
                return true;

            // Include rowIdx - 1 so a branch/call's delay slot is not annotated
            // with post-call register values. Example: jal; delay-slot; break.
            return !HasControlFlowBarrierBetween(Math.Max(0, rowIdx - 1), activeIdx);
        }

        private bool CanUseFutureBreakRow(int rowIdx, int activeIdx)
        {
            if (rowIdx < activeIdx)
                return false;
            if (rowIdx == activeIdx)
                return true;

            for (int i = activeIdx; i < rowIdx; i++)
            {
                if (!IsControlFlowBarrierRow(i))
                    continue;

                // The instruction immediately after a branch/call/jump is the delay
                // slot, so using the break snapshot can still make sense there.
                return i == activeIdx && rowIdx == activeIdx + 1;
            }

            return true;
        }

        private bool HasControlFlowBarrierBetween(int startInclusive, int endExclusive)
        {
            startInclusive = Math.Max(0, startInclusive);
            endExclusive = Math.Min(_rows.Count, endExclusive);
            for (int i = startInclusive; i < endExclusive; i++)
            {
                if (IsControlFlowBarrierRow(i))
                    return true;
            }
            return false;
        }

        private bool IsControlFlowBarrierRow(int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= _rows.Count)
                return false;

            return _rows[rowIdx].Kind is InstructionType.Branch or InstructionType.Jump or InstructionType.Call;
        }

        private static bool WritesGprForBreakSnapshot(uint word, uint reg)
        {
            if (reg == 0)
                return false;

            return WritesRegister(word, reg) || GetDestinationGpr(word) == reg;
        }

        /// <summary>
        /// Reads register <paramref name="regIndex"/> (0..31) from the cached
        /// break-time snapshot. Returns false when no snapshot is cached.
        /// </summary>
        internal bool TryGetBreakGpr(uint regIndex, out uint value)
        {
            value = 0;
            if (_breakRegisterSnapshot == null)
                return false;
            uint? v = FindGprAddress(_breakRegisterSnapshot, regIndex);
            if (!v.HasValue)
                return false;
            value = v.Value;
            return true;
        }

        /// <summary>
        /// Computes the effective memory address that the load/store or address-
        /// builder instruction on <paramref name="rowIdx"/> will use, given the
        /// live register state captured at the most recent break. Returns 0
        /// when the row is not a memory access or no live snapshot is cached.
        /// </summary>
        internal uint ComputeBreakDataAddress(uint instructionWord)
        {
            if (_breakRegisterSnapshot == null)
                return 0;

            uint w = instructionWord;
            uint op = (w >> 26) & 0x3F;
            uint rs = (w >> 21) & 0x1F;
            int si = (short)(w & 0xFFFF);
            uint ui = w & 0xFFFF;

            // LUI: not a base-register access — return the upper-half it loads.
            if (op == 0x0F)
                return ui << 16;

            bool isLoadStore = IsLoadStore(op);
            bool isAddrBuild = op is 0x08 or 0x09 or 0x0D;
            if (!isLoadStore && !isAddrBuild)
                return 0;

            if (rs == 0)
            {
                // base = $zero — the immediate IS the address.
                if (op == 0x0D) return ui;       // ori from zero: low half
                return unchecked((uint)si);      // addiu/addi/load/store from zero
            }

            if (!TryGetBreakGpr(rs, out uint baseValue))
                return 0;

            return op == 0x0D ? (baseValue | ui) : unchecked(baseValue + (uint)si);
        }

        /// <summary>
        /// Returns the GPR index that <paramref name="instructionWord"/> writes
        /// (for the purpose of the "show destination current value" annotation
        /// on the row above the break). Returns 0 when the instruction has no
        /// GPR destination, or writes to $zero (which is meaningless to show).
        /// </summary>
        internal static uint GetDestinationGpr(uint instructionWord)
        {
            uint op = (instructionWord >> 26) & 0x3F;
            uint rt = (instructionWord >> 16) & 0x1F;
            uint rd = (instructionWord >> 11) & 0x1F;
            uint fn = instructionWord & 0x3F;

            // R-type
            if (op == 0x00)
            {
                // sll/srl/sra/sllv/srlv/srav/movz/movn/mfhi/mflo/mthi/mtlo/
                // mult/div/add/addu/sub/subu/and/or/xor/nor/slt/sltu/
                // dadd/daddu/dsub/dsubu/dsll/dsrl/dsra/dsll32/dsrl32/dsra32
                switch (fn)
                {
                    case 0x00: case 0x02: case 0x03:                       // sll, srl, sra
                    case 0x04: case 0x06: case 0x07:                       // sllv, srlv, srav
                    case 0x0A: case 0x0B:                                  // movz, movn
                    case 0x10: case 0x12:                                  // mfhi, mflo
                    case 0x20: case 0x21: case 0x22: case 0x23:            // add, addu, sub, subu
                    case 0x24: case 0x25: case 0x26: case 0x27:            // and, or, xor, nor
                    case 0x2A: case 0x2B:                                  // slt, sltu
                    case 0x2C: case 0x2D: case 0x2E: case 0x2F:            // dadd, daddu, dsub, dsubu
                    case 0x14: case 0x16: case 0x17:                       // dsllv, dsrlv, dsrav
                    case 0x38: case 0x3A: case 0x3B:                       // dsll, dsrl, dsra
                    case 0x3C: case 0x3E: case 0x3F:                       // dsll32, dsrl32, dsra32
                        return rd;
                    case 0x09:                                             // jalr — writes rd (default ra)
                        return rd == 0 ? 31u : rd;
                }
                return 0;
            }

            // I-type with rt destination: addi, addiu, slti, sltiu, andi, ori,
            // xori, lui, daddi, daddiu — and all loads.
            if (op is 0x08 or 0x09 or 0x0A or 0x0B
                   or 0x0C or 0x0D or 0x0E or 0x0F
                   or 0x18 or 0x19)
                return rt;

            // jal: writes to ra
            if (op == 0x03)
                return 31;

            // Loads write rt, but only the integer-loads — lwc1 / lqc2 write
            // to FPR / VU registers, not GPRs, so skip them here.
            if (IsLoadStore(op))
            {
                bool isStore = op is 0x28 or 0x29 or 0x2A or 0x2B or 0x2C or 0x2D
                                  or 0x2E or 0x2F or 0x39 or 0x3E or 0x3F or 0x1F;
                bool isFprLoad = op is 0x31 or 0x36;
                if (!isStore && !isFprLoad)
                    return rt;
            }

            return 0;
        }


    }
}
