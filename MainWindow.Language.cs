using System.Windows;
using System.Windows.Controls;

namespace IRacingTracker;

/// <summary>Idioma: troca em tempo real, sem reiniciar.</summary>
public partial class MainWindow
{
    private void Lang_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string code)
        {
            _settings.Language = code;
            Loc.Set(code);
            ApplyLanguage();
            SaveSettings();
        }
    }

    /// <summary>Atualiza os textos que sao escritos pelo codigo (os do XAML atualizam sozinhos).</summary>
    private void ApplyLanguage()
    {
        // botoes de idioma: o ativo fica destacado
        Button[] lb = { LangBtn0, LangBtn1, LangBtn2, LangBtn3 };
        for (int i = 0; i < lb.Length; i++)
            SetBtnState(lb[i], Loc.Codes[i] == Loc.Current);

        // colunas (caixas de selecao)
        for (int i = 0; i < ColumnDefs.Length && i < ColumnsPanel.Children.Count; i++)
            if (ColumnsPanel.Children[i] is CheckBox cb) cb.Content = Loc.T(ColumnDefs[i].Label);

        UpdateExtraDirUi();
        UpdateThemeButtons();
        ApplyCompact();
        RunHistSearch();
        _forceMode = true;
        ApplyMode(_mode);

        UpdateFilterUi();
        UpdateHotkeyLabels();
        RebuildTagList();

        ClickThroughButton.Content = _clickThrough ? Loc.T("btn_ct_off") : Loc.T("btn_ct_on");
        _pill?.SetState(_clickThrough);

        if (_detailRow != null && DetailPanel.Visibility == Visibility.Visible)
        {
            var state = _chips.ToDictionary(c => c.Def.Id, c => c.IsOn);
            _chips.Clear();
            foreach (TagDef t in _tags)
                _chips.Add(new TagChipVm(t, state.TryGetValue(t.Id, out bool on) && on));
            if (_history != null) DetailText.Text = BuildDetailText(_detailRow);
        }
    }
}
