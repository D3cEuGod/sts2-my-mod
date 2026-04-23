using Godot;

namespace Sts2DpsPrototype;

internal sealed partial class DpsOverlay : CanvasLayer
{
    private VBoxContainer _body = null!;
    private VBoxContainer _mainSections = null!;
    private VBoxContainer _historyView = null!;
    private VBoxContainer _currentRows = null!;
    private VBoxContainer _lifetimeRows = null!;
    private VBoxContainer _lastCombatRows = null!;
    private VBoxContainer _historyRows = null!;
    private HBoxContainer _historyPagerRow = null!;
    private Label _summaryLabel = null!;
    private Label _lifetimeLabel = null!;
    private Label _lastCombatLabel = null!;
    private Label _historySummaryLabel = null!;
    private Label _historyPageLabel = null!;
    private Label _footerLabel = null!;
    private Button _historyOpenButton = null!;
    private PanelContainer _panel = null!;
    private Control _dragHandle = null!;
    private Button _collapseButton = null!;
    private double _refreshCooldown;
    private bool _collapsed;
    private bool _showCombatHistory;
    private bool _dragging;
    private int _historyPageIndex;
    private const int HistoryPageSize = 4;
    private readonly HashSet<int> _expandedCombatCards = new();
    private Vector2 _dragPointerOffset;
    private float _panelWidth = 300f;
    private float _expandedPanelHeight = 276f;
    private float _collapsedPanelHeight = 40f;
    private int _lastAppliedMaxRows = -1;

    private static T Passthrough<T>(T control) where T : Control
    {
        control.MouseFilter = Control.MouseFilterEnum.Ignore;
        control.FocusMode = Control.FocusModeEnum.None;
        return control;
    }

    public override void _Ready()
    {
        Layer = 1000;
        Name = "DpsOverlay";
        ProcessMode = ProcessModeEnum.Always;
        BuildUi();
        SetDefaultTopRightPosition();
        ApplyCollapsedState();
        Refresh();
        MainFile.Log.Info("DpsOverlay ready.");
    }

    public override void _Process(double delta)
    {
        Visible = PrototypeSettings.ShowPanel;
        if (!Visible)
            return;

        ApplyDynamicLayout();
        _panel.Visible = true;
        _panel.Modulate = new Color(1f, 1f, 1f, PrototypeSettings.PanelOpacity);

        _refreshCooldown -= delta;
        if (_refreshCooldown > 0)
            return;

        _refreshCooldown = 0.2;
        Refresh();
    }

    private void BuildUi()
    {
        var root = Passthrough(new Control());
        root.Name = "OverlayRoot";
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        _panel = new PanelContainer();
        _panel.Name = "DpsPanel";
        _panel.AnchorLeft = 0f;
        _panel.AnchorTop = 0f;
        _panel.AnchorRight = 0f;
        _panel.AnchorBottom = 0f;
        _panel.OffsetLeft = 0f;
        _panel.OffsetTop = 16f;
        _panel.OffsetRight = _panel.OffsetLeft + _panelWidth;
        _panel.OffsetBottom = _panel.OffsetTop + _expandedPanelHeight;
        _panel.MouseFilter = Control.MouseFilterEnum.Pass;
        _panel.Modulate = new Color(1f, 1f, 1f, PrototypeSettings.PanelOpacity);
        root.AddChild(_panel);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.1f, 0.12f, 0.9f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 9,
            ContentMarginRight = 9,
            ContentMarginTop = 7,
            ContentMarginBottom = 7,
            BorderColor = new Color(0.74f, 0.62f, 0.36f, 0.34f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            ShadowColor = new Color(0f, 0f, 0f, 0.26f),
            ShadowSize = 3,
        };
        _panel.AddThemeStyleboxOverride("panel", style);

        var shell = new VBoxContainer();
        shell.AddThemeConstantOverride("separation", 5);
        shell.MouseFilter = Control.MouseFilterEnum.Pass;
        _panel.AddChild(shell);

        var headerWrap = new HBoxContainer();
        headerWrap.AddThemeConstantOverride("separation", 4);
        headerWrap.MouseFilter = Control.MouseFilterEnum.Pass;
        shell.AddChild(headerWrap);

        _dragHandle = new Control();
        _dragHandle.Name = "DragHandle";
        _dragHandle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _dragHandle.CustomMinimumSize = new Vector2(0f, 22f);
        _dragHandle.MouseFilter = Control.MouseFilterEnum.Stop;
        _dragHandle.MouseDefaultCursorShape = Control.CursorShape.Move;
        _dragHandle.GuiInput += OnDragHandleGuiInput;
        headerWrap.AddChild(_dragHandle);

        var titleBadge = new PanelContainer();
        titleBadge.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleBadge.MouseFilter = Control.MouseFilterEnum.Ignore;
        titleBadge.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.17f, 0.1f, 0.42f),
            BorderColor = new Color(0.78f, 0.65f, 0.38f, 0.28f),
            BorderWidthBottom = 1,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3,
        });
        _dragHandle.AddChild(titleBadge);

        var title = new Label
        {
            Text = "伤害统计",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        title.AddThemeColorOverride("font_color", new Color(0.91f, 0.79f, 0.52f));
        title.AddThemeFontSizeOverride("font_size", 13);
        titleBadge.AddChild(title);

        _collapseButton = new Button
        {
            Text = "▾",
            TooltipText = "收起/展开",
            CustomMinimumSize = new Vector2(20f, 20f),
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        _collapseButton.AddThemeFontSizeOverride("font_size", 9);
        _collapseButton.AddThemeColorOverride("font_color", new Color(0.74f, 0.72f, 0.66f));
        _collapseButton.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f, 0.02f), CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 });
        _collapseButton.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f, 0.05f), CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 });
        _collapseButton.AddThemeStyleboxOverride("pressed", new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f, 0.1f), CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 });
        _collapseButton.Pressed += ToggleCollapsed;
        headerWrap.AddChild(_collapseButton);

        var headerLine = Passthrough(new ColorRect
        {
            Color = new Color(0.82f, 0.68f, 0.38f, 0.16f),
            CustomMinimumSize = new Vector2(0f, 1f),
        });
        shell.AddChild(headerLine);

        _body = new VBoxContainer();
        _body.AddThemeConstantOverride("separation", 3);
        _body.MouseFilter = Control.MouseFilterEnum.Ignore;
        shell.AddChild(_body);

        _mainSections = new VBoxContainer();
        _mainSections.AddThemeConstantOverride("separation", 3);
        _mainSections.MouseFilter = Control.MouseFilterEnum.Ignore;
        _body.AddChild(_mainSections);

        _mainSections.AddChild(BuildSectionTitle("当前战斗"));
        _summaryLabel = BuildSectionLabel();
        _mainSections.AddChild(_summaryLabel);
        _currentRows = BuildRowsContainer();
        _mainSections.AddChild(_currentRows);

        _mainSections.AddChild(BuildDivider());
        _mainSections.AddChild(BuildSectionTitle("本局累计"));
        _lifetimeLabel = BuildSectionLabel();
        _mainSections.AddChild(_lifetimeLabel);
        _lifetimeRows = BuildRowsContainer();
        _mainSections.AddChild(_lifetimeRows);

        _mainSections.AddChild(BuildDivider());
        _mainSections.AddChild(BuildHistorySectionHeader());
        _lastCombatLabel = BuildSectionLabel();
        _mainSections.AddChild(_lastCombatLabel);
        _lastCombatRows = BuildRowsContainer();
        _mainSections.AddChild(_lastCombatRows);

        _footerLabel = Passthrough(new Label
        {
            Text = "F7 显示 · F8 测试 · F9 重置",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        _footerLabel.AddThemeColorOverride("font_color", new Color(0.45f, 0.49f, 0.56f));
        _footerLabel.AddThemeFontSizeOverride("font_size", 10);
        _mainSections.AddChild(_footerLabel);

        _historyView = BuildHistoryView();
        _historyView.Visible = false;
        _body.AddChild(_historyView);
    }

    private void OnDragHandleGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                _dragging = true;
                _dragPointerOffset = mouseButton.GlobalPosition - new Vector2(_panel.OffsetLeft, _panel.OffsetTop);
                GetViewport().SetInputAsHandled();
                return;
            }

            _dragging = false;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventMouseMotion mouseMotion && _dragging)
        {
            SetPanelTopLeft(mouseMotion.GlobalPosition - _dragPointerOffset);
            GetViewport().SetInputAsHandled();
        }
    }

    private void SetPanelTopLeft(Vector2 topLeft)
    {
        Rect2 visibleRect = GetViewport().GetVisibleRect();
        float width = _panelWidth;
        float height = _collapsed ? _collapsedPanelHeight : _expandedPanelHeight;

        float minX = visibleRect.Position.X;
        float minY = visibleRect.Position.Y;
        float maxX = visibleRect.End.X - width;
        float maxY = visibleRect.End.Y - height;

        float clampedX = Mathf.Clamp(topLeft.X, minX, Math.Max(minX, maxX));
        float clampedY = Mathf.Clamp(topLeft.Y, minY, Math.Max(minY, maxY));

        _panel.OffsetLeft = clampedX;
        _panel.OffsetTop = clampedY;
        _panel.OffsetRight = clampedX + width;
        _panel.OffsetBottom = clampedY + height;
    }

    private void SetDefaultTopRightPosition()
    {
        Rect2 visibleRect = GetViewport().GetVisibleRect();
        float defaultX = visibleRect.End.X - _panelWidth - 16f;
        SetPanelTopLeft(new Vector2(defaultX, 16f));
    }

    private void ToggleCollapsed()
    {
        _collapsed = !_collapsed;
        ApplyCollapsedState();
        Refresh();
    }

    private void ApplyCollapsedState()
    {
        _collapseButton.Text = _collapsed ? "▸" : "▾";
        _body.Visible = !_collapsed;
        float panelHeight = _collapsed ? _collapsedPanelHeight : _expandedPanelHeight;
        _panel.OffsetBottom = _panel.OffsetTop + panelHeight;
    }

    private void ApplyDynamicLayout()
    {
        if (_lastAppliedMaxRows == PrototypeSettings.MaxRows)
            return;

        _lastAppliedMaxRows = PrototypeSettings.MaxRows;
        _expandedPanelHeight = 276f + Math.Max(0, PrototypeSettings.MaxRows - 2) * 24f;
        ApplyCollapsedState();
        SetPanelTopLeft(new Vector2(_panel.OffsetLeft, _panel.OffsetTop));
    }

    private static Control BuildDivider()
    {
        return Passthrough(new ColorRect
        {
            Color = new Color(1f, 1f, 1f, 0.06f),
            CustomMinimumSize = new Vector2(0f, 1f),
        });
    }

    private static Label BuildSectionTitle(string text)
    {
        var label = Passthrough(new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Left,
        });
        label.AddThemeColorOverride("font_color", new Color(0.75f, 0.78f, 0.82f));
        label.AddThemeFontSizeOverride("font_size", 12);
        return label;
    }

    private static Label BuildSectionLabel()
    {
        var label = Passthrough(new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        label.AddThemeColorOverride("font_color", new Color(0.66f, 0.64f, 0.6f));
        label.AddThemeFontSizeOverride("font_size", 11);
        return label;
    }

    private static VBoxContainer BuildRowsContainer()
    {
        var box = Passthrough(new VBoxContainer());
        box.AddThemeConstantOverride("separation", 1);
        return box;
    }

    private Control BuildHistorySectionHeader()
    {
        var row = Passthrough(new HBoxContainer());
        row.AddThemeConstantOverride("separation", 4);

        _historyOpenButton = new Button
        {
            Text = "上一场结算 ▸",
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            TooltipText = "查看本局更早战斗记录",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
        };
        _historyOpenButton.AddThemeFontSizeOverride("font_size", 12);
        _historyOpenButton.AddThemeColorOverride("font_color", new Color(0.75f, 0.78f, 0.82f));
        _historyOpenButton.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        _historyOpenButton.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
        _historyOpenButton.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
        _historyOpenButton.Pressed += OpenCombatHistory;
        row.AddChild(_historyOpenButton);

        var hint = Passthrough(new Label { Text = "查看本局内更早战斗" });
        hint.AddThemeColorOverride("font_color", new Color(0.47f, 0.49f, 0.5f));
        hint.AddThemeFontSizeOverride("font_size", 10);
        row.AddChild(hint);
        row.AddChild(Passthrough(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }));

        return row;
    }

    private VBoxContainer BuildHistoryView()
    {
        var view = new VBoxContainer();
        view.AddThemeConstantOverride("separation", 4);
        view.MouseFilter = Control.MouseFilterEnum.Ignore;

        var topRow = Passthrough(new HBoxContainer());
        topRow.AddThemeConstantOverride("separation", 4);
        view.AddChild(topRow);

        var backButton = new Button
        {
            Text = "‹ 返回",
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            TooltipText = "返回主面板",
        };
        backButton.AddThemeFontSizeOverride("font_size", 11);
        backButton.Pressed += CloseCombatHistory;
        topRow.AddChild(backButton);

        var title = Passthrough(new Label { Text = "本局战斗记录", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        title.AddThemeColorOverride("font_color", new Color(0.91f, 0.79f, 0.52f));
        title.AddThemeFontSizeOverride("font_size", 13);
        topRow.AddChild(title);

        _historySummaryLabel = BuildSectionLabel();
        view.AddChild(_historySummaryLabel);

        _historyPagerRow = Passthrough(new HBoxContainer());
        _historyPagerRow.AddThemeConstantOverride("separation", 6);
        view.AddChild(_historyPagerRow);

        var prevButton = new Button
        {
            Text = "‹ 上一页",
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        prevButton.AddThemeFontSizeOverride("font_size", 10);
        prevButton.Pressed += PrevHistoryPage;
        _historyPagerRow.AddChild(prevButton);

        _historyPageLabel = Passthrough(new Label { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center });
        _historyPageLabel.AddThemeColorOverride("font_color", new Color(0.58f, 0.58f, 0.55f));
        _historyPageLabel.AddThemeFontSizeOverride("font_size", 10);
        _historyPagerRow.AddChild(_historyPageLabel);

        var nextButton = new Button
        {
            Text = "下一页 ›",
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        nextButton.AddThemeFontSizeOverride("font_size", 10);
        nextButton.Pressed += NextHistoryPage;
        _historyPagerRow.AddChild(nextButton);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.CustomMinimumSize = new Vector2(0f, 220f);
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        scroll.MouseFilter = Control.MouseFilterEnum.Pass;
        view.AddChild(scroll);

        _historyRows = new VBoxContainer();
        _historyRows.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _historyRows.AddThemeConstantOverride("separation", 6);
        _historyRows.MouseFilter = Control.MouseFilterEnum.Ignore;
        scroll.AddChild(_historyRows);

        return view;
    }

    private void OpenCombatHistory()
    {
        _showCombatHistory = true;
        _historyPageIndex = 0;
        Refresh();
    }

    private void CloseCombatHistory()
    {
        _showCombatHistory = false;
        Refresh();
    }

    private void PrevHistoryPage()
    {
        if (_historyPageIndex <= 0)
            return;

        _historyPageIndex--;
        RefreshHistoryView();
    }

    private void NextHistoryPage()
    {
        int pageCount = GetHistoryPageCount();
        if (_historyPageIndex >= pageCount - 1)
            return;

        _historyPageIndex++;
        RefreshHistoryView();
    }

    private int GetHistoryPageCount()
    {
        int total = DpsTracker.GetHistoricalCombatRecords().Count;
        return Math.Max(1, (int)Math.Ceiling(total / (double)HistoryPageSize));
    }

    private void RefreshHistoryView()
    {
        var allRecords = DpsTracker.GetHistoricalCombatRecords();
        int pageCount = Math.Max(1, (int)Math.Ceiling(allRecords.Count / (double)HistoryPageSize));
        _historyPageIndex = Math.Clamp(_historyPageIndex, 0, pageCount - 1);

        _historySummaryLabel.Text = DpsTracker.GetCombatHistorySummary();
        _historyPagerRow.Visible = allRecords.Count > 0;
        _historyPageLabel.Text = allRecords.Count == 0 ? string.Empty : $"第 {_historyPageIndex + 1}/{pageCount} 页";

        foreach (Node child in _historyRows.GetChildren())
            child.QueueFree();

        if (allRecords.Count == 0)
        {
            _historyRows.AddChild(BuildEmptyLabel("还没有可查看的更早战斗记录。"));
            return;
        }

        var pageRecords = allRecords
            .Skip(_historyPageIndex * HistoryPageSize)
            .Take(HistoryPageSize)
            .ToArray();

        foreach (var record in pageRecords)
            _historyRows.AddChild(BuildHistoryRecord(record, _expandedCombatCards.Contains(record.CombatIndex)));
    }

    private Control BuildHistoryRecord(DpsTracker.CombatRecord record, bool expanded)
    {
        var card = new VBoxContainer();
        card.AddThemeConstantOverride("separation", 3);
        card.MouseFilter = Control.MouseFilterEnum.Pass;

        var panel = new PanelContainer();
        panel.MouseFilter = Control.MouseFilterEnum.Pass;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(1f, 1f, 1f, 0.025f),
            BorderColor = new Color(1f, 1f, 1f, 0.06f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 7,
            ContentMarginRight = 7,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        });
        card.AddChild(panel);

        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 3);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        panel.AddChild(body);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 4);
        header.MouseFilter = Control.MouseFilterEnum.Pass;
        body.AddChild(header);

        var expandButton = new Button
        {
            Text = expanded ? "▾" : "▸",
            FocusMode = Control.FocusModeEnum.None,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            CustomMinimumSize = new Vector2(20f, 20f),
            TooltipText = expanded ? "收起本场详情" : "展开查看更多玩家",
        };
        expandButton.AddThemeFontSizeOverride("font_size", 10);
        expandButton.Pressed += () => ToggleCombatRecordExpanded(record.CombatIndex);
        header.AddChild(expandButton);

        var title = Passthrough(new Label
        {
            Text = $"第 {record.CombatIndex} 场",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });
        title.AddThemeColorOverride("font_color", new Color(0.87f, 0.86f, 0.82f));
        title.AddThemeFontSizeOverride("font_size", 12);
        header.AddChild(title);

        var total = Passthrough(new Label
        {
            Text = $"{record.TotalDamage:F0}",
        });
        total.AddThemeColorOverride("font_color", new Color(0.66f, 0.73f, 0.8f));
        total.AddThemeFontSizeOverride("font_size", 12);
        header.AddChild(total);

        var summary = Passthrough(new Label
        {
            Text = $"{record.RoundCount} 回合 · 出伤 {record.ActiveDealers} 人 · 最高单次 {record.HighestSingleHit:F0}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        summary.AddThemeColorOverride("font_color", new Color(0.58f, 0.58f, 0.55f));
        summary.AddThemeFontSizeOverride("font_size", 10);
        body.AddChild(summary);

        int visiblePlayers = expanded ? Math.Max(3, PrototypeSettings.MaxRows + 1) : 3;
        var snapshots = record.Snapshots.Where(snapshot => snapshot.TotalDamage > 0f).Take(visiblePlayers).ToArray();
        foreach (var snapshot in snapshots)
        {
            var row = Passthrough(new Label
            {
                Text = $"• {snapshot.DisplayName}  {snapshot.TotalDamage:F0}  /  {snapshot.DamagePerTurn:F1} DPT  /  最高 {snapshot.HighestSingleHit:F0}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
            row.AddThemeColorOverride("font_color", new Color(0.69f, 0.74f, 0.8f));
            row.AddThemeFontSizeOverride("font_size", 10);
            body.AddChild(row);
        }

        int hiddenPlayers = record.Snapshots.Count(snapshot => snapshot.TotalDamage > 0f) - snapshots.Length;
        if (!expanded && hiddenPlayers > 0)
        {
            var moreLabel = Passthrough(new Label
            {
                Text = $"还有 {hiddenPlayers} 位玩家，点击展开查看",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
            moreLabel.AddThemeColorOverride("font_color", new Color(0.47f, 0.49f, 0.5f));
            moreLabel.AddThemeFontSizeOverride("font_size", 10);
            body.AddChild(moreLabel);
        }

        return card;
    }

    private void ToggleCombatRecordExpanded(int combatIndex)
    {
        if (!_expandedCombatCards.Add(combatIndex))
            _expandedCombatCards.Remove(combatIndex);

        RefreshHistoryView();
    }

    private void Refresh()
    {
        if (!Visible)
            return;

        _summaryLabel.Text = DpsTracker.GetEncounterSummary();
        _lifetimeLabel.Text = DpsTracker.GetLifetimeSummary();
        _lastCombatLabel.Text = DpsTracker.GetLastCombatSummary();
        _footerLabel.Visible = !_collapsed;
        _mainSections.Visible = !_showCombatHistory;
        _historyView.Visible = _showCombatHistory && !_collapsed;
        _historyOpenButton.Text = _showCombatHistory ? "上一场结算 ◂" : "上一场结算 ▸";

        if (_collapsed)
            return;

        int currentRows = Math.Max(2, PrototypeSettings.MaxRows);
        int compactRows = Math.Max(1, Math.Min(2, PrototypeSettings.MaxRows - 1));

        RebuildRows(_currentRows, DpsTracker.GetSnapshots(currentRows), showDps: true, emptyText: "本场还没有有效伤害。", showRecentHit: true, accent: RowAccent.Primary, compact: false);
        RebuildRows(_lifetimeRows, DpsTracker.GetLifetimeSnapshots(compactRows), showDps: false, emptyText: "还没有累计伤害。", showRecentHit: false, accent: RowAccent.Secondary, compact: true);
        RebuildRows(_lastCombatRows, DpsTracker.GetLastCombatSnapshots(compactRows), showDps: false, emptyText: "还没有上一场结算。", showRecentHit: false, accent: RowAccent.Muted, compact: true);
        RefreshHistoryView();
    }

    private static void RebuildRows(VBoxContainer container, IReadOnlyList<DpsTracker.PlayerSnapshot> snapshots, bool showDps, string emptyText, bool showRecentHit, RowAccent accent, bool compact)
    {
        foreach (Node child in container.GetChildren())
            child.QueueFree();

        if (snapshots.Count == 0)
        {
            container.AddChild(BuildEmptyLabel(emptyText));
            return;
        }

        int rank = 1;
        foreach (var snapshot in snapshots)
        {
            container.AddChild(BuildRow(rank, snapshot, showDps, showRecentHit, accent, compact));
            rank++;
        }
    }

    private static Label BuildEmptyLabel(string text)
    {
        var label = Passthrough(new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        label.AddThemeColorOverride("font_color", new Color(0.58f, 0.58f, 0.55f));
        label.AddThemeFontSizeOverride("font_size", 11);
        return label;
    }

    private static Control BuildRow(int rank, DpsTracker.PlayerSnapshot snapshot, bool showDps, bool showRecentHit, RowAccent accent, bool compact)
    {
        var box = Passthrough(new VBoxContainer());
        box.AddThemeConstantOverride("separation", compact ? 0 : 1);

        var header = Passthrough(new HBoxContainer());
        header.AddThemeConstantOverride("separation", 4);

        var rankLabel = Passthrough(new Label { Text = $"#{rank}" });
        rankLabel.CustomMinimumSize = new Vector2(22f, 0f);
        rankLabel.AddThemeColorOverride("font_color", new Color(0.78f, 0.65f, 0.38f));
        rankLabel.AddThemeFontSizeOverride("font_size", 12);
        header.AddChild(rankLabel);

        var nameLabel = Passthrough(new Label
        {
            Text = snapshot.DisplayName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });
        nameLabel.AddThemeColorOverride("font_color", new Color(0.87f, 0.86f, 0.82f));
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        header.AddChild(nameLabel);

        string rightText = showDps
            ? (snapshot.TotalDamage > 0f ? $"{snapshot.DamagePerTurn:F1} DPT" : "待命")
            : $"{snapshot.TotalDamage:F0}";
        var rightLabel = Passthrough(new Label { Text = rightText });
        rightLabel.AddThemeColorOverride("font_color", accent switch
        {
            RowAccent.Primary => new Color(0.78f, 0.84f, 0.62f),
            RowAccent.Secondary => new Color(0.66f, 0.73f, 0.8f),
            _ => new Color(0.53f, 0.58f, 0.62f),
        });
        rightLabel.AddThemeFontSizeOverride("font_size", 12);
        header.AddChild(rightLabel);

        box.AddChild(header);

        if (!compact)
        {
            string detailText = $"总伤害 {snapshot.TotalDamage:F0} · 最高单次 {snapshot.HighestSingleHit:F0}";
            var detail = Passthrough(new Label
            {
                Text = detailText,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
            detail.AddThemeColorOverride("font_color", new Color(0.47f, 0.49f, 0.5f));
            detail.AddThemeFontSizeOverride("font_size", 10);
            box.AddChild(detail);
        }

        return box;
    }

    private enum RowAccent
    {
        Primary,
        Secondary,
        Muted,
    }
}
