using DesktopPet.Core.Reminders;
using Godot;
using System;
using System.Linq;

namespace DesktopPet.App.Reminders;

public partial class ReminderEditorView : PanelContainer
{
    public event Action<ReminderDefinition>? SaveRequested;
    public event Action<Guid>? DeleteRequested;
    public event Action<Guid, bool>? TaskEnabledChanged;

    private ReminderDocument _document = ReminderDocument.CreateDefault();
    private ReminderDefinition? _editing;
    private VBoxContainer _listPage = null!;
    private VBoxContainer _taskRows = null!;
    private VBoxContainer _editPage = null!;
    private Label _capacity = null!;
    private Label _count = null!;
    private Label _validation = null!;
    private Button _newButton = null!;
    private Button _deleteButton = null!;
    private Button _saveButton = null!;
    private TextEdit _text = null!;
    private OptionButton _mode = null!;
    private OptionButton _repeat = null!;
    private SpinBox _countdownValue = null!;
    private OptionButton _countdownUnit = null!;
    private LineEdit _date = null!;
    private LineEdit _time = null!;
    private OptionButton _weekday = null!;
    private CheckButton _enabled = null!;
    private HBoxContainer _dateRow = null!;
    private HBoxContainer _countdownRow = null!;
    private bool _normalizingText;

    public override void _Ready()
    {
        Theme = ReminderTheme.CreateTheme();
        AddThemeStyleboxOverride(
            "panel",
            ReminderTheme.RoundedBox(ReminderTheme.Surface, ReminderTheme.Line, 20));
        BuildUi();
        ShowList();
    }

    public void SetDocument(ReminderDocument document)
    {
        _document = document;
        if (IsNodeReady())
            RebuildTaskRows();
    }

    public void BeginNewReminder() => BeginNew();

    public void ShowTaskList() => ShowList();

    public void SetDraftText(string text)
    {
        _text.Text = text;
        RefreshValidation();
    }

    private void BuildUi()
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        var sprout = new Label { Text = "♧", CustomMinimumSize = new Vector2(30, 30) };
        sprout.AddThemeColorOverride("font_color", ReminderTheme.MintDeep);
        sprout.AddThemeFontSizeOverride("font_size", 22);
        header.AddChild(sprout);
        var heading = new Label
        {
            Text = "提醒备忘录",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        heading.AddThemeFontSizeOverride("font_size", 18);
        header.AddChild(heading);
        _capacity = new Label { VerticalAlignment = VerticalAlignment.Center };
        _capacity.AddThemeColorOverride("font_color", ReminderTheme.Muted);
        header.AddChild(_capacity);
        root.AddChild(header);

        var separator = new HSeparator();
        separator.AddThemeColorOverride("separator", ReminderTheme.Line);
        root.AddChild(separator);

        _listPage = BuildListPage();
        root.AddChild(_listPage);
        _editPage = BuildEditPage();
        root.AddChild(_editPage);
    }

    private VBoxContainer BuildListPage()
    {
        var page = new VBoxContainer();
        page.AddThemeConstantOverride("separation", 10);
        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 8);
        var state = new Label
        {
            Text = "● 备忘录提醒状态由任务栏总开关控制",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        state.AddThemeColorOverride("font_color", ReminderTheme.Muted);
        toolbar.AddChild(state);
        _newButton = new Button { Text = "＋ 新建提醒" };
        ReminderTheme.StylePrimary(_newButton);
        _newButton.Pressed += BeginNew;
        toolbar.AddChild(_newButton);
        page.AddChild(toolbar);

        _taskRows = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _taskRows.AddThemeConstantOverride("separation", 8);
        page.AddChild(_taskRows);
        return page;
    }

    private VBoxContainer BuildEditPage()
    {
        var page = new VBoxContainer
        {
            Visible = false,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        page.AddThemeConstantOverride("separation", 8);

        var textHeader = new HBoxContainer();
        textHeader.AddChild(new Label { Text = "提醒内容", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _count = new Label();
        _count.AddThemeColorOverride("font_color", ReminderTheme.Muted);
        textHeader.AddChild(_count);
        page.AddChild(textHeader);

        _text = new TextEdit
        {
            CustomMinimumSize = new Vector2(0, 88),
            WrapMode = TextEdit.LineWrappingMode.Boundary
        };
        _text.TextChanged += NormalizeDraftText;
        page.AddChild(_text);

        page.AddChild(new Label { Text = "提醒方式" });
        _mode = new OptionButton();
        _mode.AddItem("定时提醒", (int)ReminderMode.Scheduled);
        _mode.AddItem("倒数提醒", (int)ReminderMode.Countdown);
        _mode.ItemSelected += _ => RefreshModeFields();
        page.AddChild(_mode);

        _dateRow = new HBoxContainer();
        _dateRow.AddThemeConstantOverride("separation", 8);
        _date = new LineEdit
        {
            PlaceholderText = "日期 YYYY-MM-DD",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _date.TextChanged += _ => RefreshValidation();
        _dateRow.AddChild(_date);
        _time = new LineEdit
        {
            PlaceholderText = "时间 HH:mm",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _time.TextChanged += _ => RefreshValidation();
        _dateRow.AddChild(_time);
        _weekday = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var day in new[] { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" })
            _weekday.AddItem(day);
        _weekday.ItemSelected += _ => RefreshValidation();
        _dateRow.AddChild(_weekday);
        page.AddChild(_dateRow);

        _countdownRow = new HBoxContainer();
        _countdownRow.AddThemeConstantOverride("separation", 8);
        _countdownValue = new SpinBox
        {
            MinValue = 1,
            MaxValue = 525600,
            Value = 40,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _countdownValue.ValueChanged += _ => RefreshValidation();
        _countdownRow.AddChild(_countdownValue);
        _countdownUnit = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _countdownUnit.AddItem("分钟", (int)CountdownUnit.Minutes);
        _countdownUnit.AddItem("小时", (int)CountdownUnit.Hours);
        _countdownUnit.AddItem("天", (int)CountdownUnit.Days);
        _countdownUnit.ItemSelected += _ => RefreshValidation();
        _countdownRow.AddChild(_countdownUnit);
        page.AddChild(_countdownRow);

        var repeatRow = new HBoxContainer();
        repeatRow.AddThemeConstantOverride("separation", 8);
        repeatRow.AddChild(new Label { Text = "重复方式", VerticalAlignment = VerticalAlignment.Center });
        _repeat = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _repeat.ItemSelected += _ => RefreshModeFields();
        repeatRow.AddChild(_repeat);
        _enabled = new CheckButton { Text = "开启此事项", ButtonPressed = true };
        repeatRow.AddChild(_enabled);
        page.AddChild(repeatRow);

        _validation = new Label();
        _validation.AddThemeColorOverride("font_color", ReminderTheme.Danger);
        page.AddChild(_validation);

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        _deleteButton = new Button
        {
            Text = "删除此事项",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _deleteButton.AddThemeColorOverride("font_color", ReminderTheme.Danger);
        _deleteButton.Flat = true;
        _deleteButton.Pressed += () =>
        {
            if (_editing is not null)
                DeleteRequested?.Invoke(_editing.Id);
        };
        actions.AddChild(_deleteButton);
        var cancel = new Button { Text = "取消" };
        ReminderTheme.StyleSecondary(cancel);
        cancel.Pressed += ShowList;
        actions.AddChild(cancel);
        _saveButton = new Button { Text = "保存提醒" };
        ReminderTheme.StylePrimary(_saveButton);
        _saveButton.Pressed += SaveEditing;
        actions.AddChild(_saveButton);
        page.AddChild(actions);
        return page;
    }

    private void RebuildTaskRows()
    {
        foreach (var child in _taskRows.GetChildren())
            child.QueueFree();

        foreach (var item in _document.Items.OrderBy(task => task.Order))
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            var edit = new Button
            {
                Text = $"♡  {item.Text}\n    {Describe(item)}",
                Alignment = HorizontalAlignment.Left,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 58)
            };
            ReminderTheme.StyleSecondary(edit);
            edit.Pressed += () => BeginEdit(item);
            row.AddChild(edit);
            var enabled = new CheckButton { Text = "开启", ButtonPressed = item.Enabled };
            enabled.Toggled += active => TaskEnabledChanged?.Invoke(item.Id, active);
            row.AddChild(enabled);
            _taskRows.AddChild(row);
        }

        if (_document.Items.Count < ReminderDocument.MaxItems)
        {
            var remaining = ReminderDocument.MaxItems - _document.Items.Count;
            var empty = new Label
            {
                Text = $"还可以添加 {remaining} 个提醒事项 ✦",
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, 42),
                VerticalAlignment = VerticalAlignment.Center
            };
            empty.AddThemeColorOverride("font_color", ReminderTheme.Muted);
            _taskRows.AddChild(empty);
        }

        _capacity.Text = $"{_document.Items.Count} / {ReminderDocument.MaxItems} 个事项";
        _newButton.Disabled = _document.Items.Count >= ReminderDocument.MaxItems;
    }

    private void BeginNew()
    {
        if (_document.Items.Count >= ReminderDocument.MaxItems)
            return;
        BeginEdit(ReminderDefinition.CreateDefault() with
        {
            Id = Guid.NewGuid(),
            Text = string.Empty,
            Order = _document.NextOrder(),
            HasTriggered = false
        });
        _deleteButton.Visible = false;
    }

    private void BeginEdit(ReminderDefinition item)
    {
        _editing = item;
        _text.Text = item.Text;
        _enabled.ButtonPressed = item.Enabled;
        _mode.Select(item.Mode == ReminderMode.Scheduled ? 0 : 1);
        _countdownValue.Value = Math.Max(1, item.CountdownValue);
        _countdownUnit.Select((int)item.CountdownUnit);
        _date.Text = item.ScheduledDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        _time.Text = item.ScheduledTime?.ToString("HH:mm") ?? "09:00";
        _weekday.Select((int)(item.WeeklyDay ?? DayOfWeek.Monday));
        PopulateRepeat(item.Repeat);
        _deleteButton.Visible = _document.Items.Any(existing => existing.Id == item.Id);
        _listPage.Visible = false;
        _editPage.Visible = true;
        RefreshModeFields();
        RefreshValidation();
        _text.GrabFocus();
    }

    private void PopulateRepeat(ReminderRepeat selected)
    {
        _repeat.Clear();
        if (SelectedMode() == ReminderMode.Countdown)
        {
            _repeat.AddItem("仅一次", (int)ReminderRepeat.Once);
            _repeat.AddItem("每隔相同时长循环", (int)ReminderRepeat.Interval);
        }
        else
        {
            _repeat.AddItem("仅一次", (int)ReminderRepeat.Once);
            _repeat.AddItem("每天", (int)ReminderRepeat.Daily);
            _repeat.AddItem("工作日", (int)ReminderRepeat.Workdays);
            _repeat.AddItem("每周", (int)ReminderRepeat.Weekly);
        }

        for (var index = 0; index < _repeat.ItemCount; index++)
        {
            if (_repeat.GetItemId(index) == (int)selected)
            {
                _repeat.Select(index);
                return;
            }
        }
        _repeat.Select(0);
    }

    private void RefreshModeFields()
    {
        var previousRepeat = _repeat.ItemCount > 0
            ? (ReminderRepeat)_repeat.GetSelectedId()
            : ReminderRepeat.Once;
        PopulateRepeat(previousRepeat);
        var scheduled = SelectedMode() == ReminderMode.Scheduled;
        _dateRow.Visible = scheduled;
        _countdownRow.Visible = !scheduled;
        _date.Visible = scheduled && SelectedRepeat() == ReminderRepeat.Once;
        _weekday.Visible = scheduled && SelectedRepeat() == ReminderRepeat.Weekly;
        RefreshValidation();
    }

    private void RefreshValidation()
    {
        var characters = ReminderDefinition.CountTextElements(_text.Text);
        _count.Text = $"{characters} / {ReminderDefinition.MaxTextElements}";
        var built = TryBuildDefinition(out var item, out var message);
        _validation.Text = message;
        _saveButton.Disabled = !built ||
            ReminderDefinition.Validate(item!) != ReminderValidationError.None;
    }

    private void NormalizeDraftText()
    {
        if (_normalizingText)
            return;

        if (ReminderDefinition.CountTextElements(_text.Text) > ReminderDefinition.MaxTextElements)
        {
            _normalizingText = true;
            _text.Text = ReminderDefinition.TrimTextElements(
                _text.Text,
                ReminderDefinition.MaxTextElements);
            var lastLine = Math.Max(0, _text.GetLineCount() - 1);
            _text.SetCaretLine(lastLine);
            _text.SetCaretColumn(_text.GetLine(lastLine).Length);
            _normalizingText = false;
        }

        RefreshValidation();
    }

    private bool TryBuildDefinition(out ReminderDefinition? item, out string message)
    {
        item = null;
        message = string.Empty;
        if (_editing is null)
            return false;

        var text = _text.Text.Trim();
        var count = ReminderDefinition.CountTextElements(text);
        if (count == 0)
        {
            message = "请输入提醒内容";
            return false;
        }
        if (count > ReminderDefinition.MaxTextElements)
        {
            message = "提醒内容最多 200 个字符";
            return false;
        }

        var mode = SelectedMode();
        var repeat = SelectedRepeat();
        DateOnly? date = null;
        TimeOnly? time = null;
        if (mode == ReminderMode.Scheduled)
        {
            if (!TimeOnly.TryParse(_time.Text, out var parsedTime))
            {
                message = "请输入有效时间，例如 09:30";
                return false;
            }
            time = parsedTime;
            var parsedDate = default(DateOnly);
            if (repeat == ReminderRepeat.Once && !DateOnly.TryParse(_date.Text, out parsedDate))
            {
                message = "请输入有效日期，例如 2026-08-03";
                return false;
            }
            if (repeat == ReminderRepeat.Once)
            {
                date = parsedDate;
                if (parsedDate.ToDateTime(parsedTime) <= DateTime.Now)
                {
                    message = "一次性提醒请选择未来时间";
                    return false;
                }
            }
        }

        item = _editing with
        {
            Text = text,
            Enabled = _enabled.ButtonPressed,
            Mode = mode,
            Repeat = repeat,
            ScheduledDate = date,
            ScheduledTime = time,
            WeeklyDay = repeat == ReminderRepeat.Weekly
                ? (DayOfWeek)_weekday.GetSelectedId()
                : null,
            CountdownValue = mode == ReminderMode.Countdown
                ? (int)_countdownValue.Value
                : 0,
            CountdownUnit = (CountdownUnit)_countdownUnit.GetSelectedId(),
            HasTriggered = false
        };
        return ReminderDefinition.Validate(item) == ReminderValidationError.None;
    }

    private void SaveEditing()
    {
        if (TryBuildDefinition(out var item, out _) && item is not null)
            SaveRequested?.Invoke(item);
    }

    private void ShowList()
    {
        _editing = null;
        _editPage.Visible = false;
        _listPage.Visible = true;
        RebuildTaskRows();
    }

    private ReminderMode SelectedMode() => (ReminderMode)_mode.GetSelectedId();
    private ReminderRepeat SelectedRepeat() => (ReminderRepeat)_repeat.GetSelectedId();

    private static string Describe(ReminderDefinition item)
    {
        if (item.Mode == ReminderMode.Countdown)
        {
            var unit = item.CountdownUnit switch
            {
                CountdownUnit.Minutes => "分钟",
                CountdownUnit.Hours => "小时",
                _ => "天"
            };
            return item.Repeat == ReminderRepeat.Interval
                ? $"倒数 {item.CountdownValue} {unit} · 循环"
                : $"倒数 {item.CountdownValue} {unit} · 仅一次";
        }

        var time = item.ScheduledTime?.ToString("HH:mm") ?? "--:--";
        return item.Repeat switch
        {
            ReminderRepeat.Daily => $"每天 {time}",
            ReminderRepeat.Workdays => $"工作日 {time}",
            ReminderRepeat.Weekly => $"每周 {WeekdayName(item.WeeklyDay)} {time}",
            _ => $"{item.ScheduledDate:yyyy-MM-dd} {time} · 仅一次"
        };
    }

    private static string WeekdayName(DayOfWeek? day) => day switch
    {
        DayOfWeek.Monday => "星期一",
        DayOfWeek.Tuesday => "星期二",
        DayOfWeek.Wednesday => "星期三",
        DayOfWeek.Thursday => "星期四",
        DayOfWeek.Friday => "星期五",
        DayOfWeek.Saturday => "星期六",
        _ => "星期日"
    };
}
