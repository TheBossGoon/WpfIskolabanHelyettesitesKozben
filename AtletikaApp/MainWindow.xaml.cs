using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AtletikaApp;

public partial class MainWindow : Window
{
    private const string CsvHeader = "Rajtszam;Nev;Versenyszam;Nem;Eletkor;OR;VR;ER";
    private static readonly string[] Versenyszamok = ["100 m", "200 m", "400 m", "800 m", "1500 m", "Távolugrás", "Magasugrás", "Súlylökés"];
    public ObservableCollection<Versenyzo> Versenyzok { get; } = [];
    private ICollectionView? _nezet;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _nezet = CollectionViewSource.GetDefaultView(Versenyzok);
        _nezet.Filter = SzuroFeltetel;
        FrissitLetszam();
    }

    private void Eletkor_Valtozott(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (txtEletkor is not null) txtEletkor.Text = $"{(int)Math.Round(e.NewValue)} év";
    }

    private void Regisztral_Click(object sender, RoutedEventArgs e)
    {
        string nev = txtNev.Text.Trim();
        if (nev.Length == 0) { Hiba("A versenyző nevének megadása kötelező."); return; }
        if (nev.Contains(';') || nev.Contains('|')) { Hiba("A névben pontosvessző és függőleges vonal nem használható."); return; }
        if (cmbVersenyszam.SelectedItem is not ComboBoxItem versenyszamItem) { Hiba("Válasszon versenyszámot."); return; }
        if (rbFerfi.IsChecked != true && rbNo.IsChecked != true) { Hiba("Adja meg a versenyző nemét."); return; }

        int rajtszam;
        string igenyelt = txtRajtszam.Text.Trim();
        if (igenyelt.Length > 0)
        {
            if (!int.TryParse(igenyelt, NumberStyles.None, CultureInfo.InvariantCulture, out rajtszam) || rajtszam is < 1 or > 9999)
            { Hiba("A rajtszám 1 és 9999 közötti egész szám lehet."); return; }
            if (Versenyzok.Any(v => v.Rajtszam == rajtszam)) { Hiba("A kért rajtszám már foglalt. Adjon meg másikat, vagy hagyja üresen a mezőt."); return; }
        }
        else
        {
            var foglalt = Versenyzok.Select(v => v.Rajtszam).ToHashSet();
            rajtszam = Enumerable.Range(1, 9999).FirstOrDefault(n => !foglalt.Contains(n));
            if (rajtszam == 0) { Hiba("Nincs több szabad rajtszám."); return; }
        }

        var versenyzo = new Versenyzo
        {
            Rajtszam = rajtszam, Nev = nev, Versenyszam = versenyszamItem.Content?.ToString() ?? "",
            Nem = rbFerfi.IsChecked == true ? "F" : "N", Eletkor = (int)Math.Round(sldEletkor.Value),
            OR = chkOR.IsChecked == true, VR = chkVR.IsChecked == true, ER = chkER.IsChecked == true
        };
        Versenyzok.Add(versenyzo);
        _nezet?.Refresh(); FrissitLetszam();
        txtNev.Clear(); txtRajtszam.Clear();
        MessageBox.Show(this, $"A regisztráció sikeres. Kiosztott rajtszám: {rajtszam}", "Sikeres regisztráció", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private bool SzuroFeltetel(object item)
    {
        if (item is not Versenyzo v) return false;
        return cmbRekordSzuro?.SelectedIndex switch { 1 => v.OR, 2 => v.VR, 3 => v.ER, _ => true };
    }

    private void Szuro_Valtozott(object sender, SelectionChangedEventArgs e)
    {
        if (_nezet is null) return;
        _nezet.Refresh(); FrissitLetszam();
    }

    private void FrissitLetszam()
    {
        if (txtLetszam is null) return;
        int lathato = _nezet?.Cast<object>().Count() ?? 0;
        txtLetszam.Text = $"Látható: {lathato} / Összesen: {Versenyzok.Count}";
    }

    private void Mentes_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { FileName = "atletika.csv", Filter = "CSV-fájl (*.csv)|*.csv|Szövegfájl (*.txt)|*.txt", FilterIndex = 1, AddExtension = false, OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        string ext = Path.GetExtension(dialog.FileName);
        string expected = dialog.FilterIndex == 1 ? ".csv" : ".txt";
        if (ext.Length == 0) dialog.FileName += expected;
        else if (!ext.Equals(expected, StringComparison.OrdinalIgnoreCase))
        { Hiba($"A kiválasztott fájltípus ({expected}) és a fájlnév kiterjesztése ({ext}) nem egyezik."); return; }
        try
        {
            if (dialog.FilterIndex == 1)
            {
                var lines = new List<string> { CsvHeader };
                lines.AddRange(Versenyzok.Select(v => string.Join(";", new object[] { v.Rajtszam, v.Nev, v.Versenyszam, v.Nem, v.Eletkor, Jel(v.OR), Jel(v.VR), Jel(v.ER) })));
                File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(false));
            }
            else File.WriteAllText(dialog.FileName, string.Join(Environment.NewLine + Environment.NewLine, Versenyzok.Select(TxtBlokk)), new UTF8Encoding(false));
        }
        catch (Exception ex) { Hiba($"A fájl mentése nem sikerült: {ex.Message}"); }
    }

    private void Betoltes_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV-fájl (*.csv)|*.csv", CheckFileExists = true, Multiselect = false };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
            var ujAdatok = new List<Versenyzo>();
            var foglalt = new HashSet<int>();
            foreach (string line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] m = line.Split(';');
                if (m.Length != 8) throw new InvalidDataException("Egy adatsor nem tartalmaz nyolc mezőt.");
                var v = new Versenyzo
                {
                    Rajtszam = int.Parse(m[0], CultureInfo.InvariantCulture), Nev = m[1], Versenyszam = m[2], Nem = m[3],
                    Eletkor = int.Parse(m[4], CultureInfo.InvariantCulture), OR = m[5] == "1", VR = m[6] == "1", ER = m[7] == "1"
                };
                if (!foglalt.Add(v.Rajtszam)) { Hiba($"A fájlban ismétlődő rajtszám szerepel: {v.Rajtszam}. A korábbi adatok változatlanok maradtak."); return; }
                ujAdatok.Add(v);
            }
            Versenyzok.Clear(); foreach (var v in ujAdatok) Versenyzok.Add(v);
            _nezet?.Refresh(); FrissitLetszam();
        }
        catch (Exception ex) { Hiba($"A fájl betöltése nem sikerült: {ex.Message}"); }
    }

    private static string Jel(bool value) => value ? "1" : "0";
    private static string TxtBlokk(Versenyzo v)
    {
        string nem = v.Nem == "F" ? "férfi" : "nő";
        string rekord(bool yes, string record) => yes ? $"{record} rendelkezik" : $"{record} nem rendelkezik";
        return $"{v.Nev}{Environment.NewLine}A {v.Eletkor} éves {nem} versenyző a(z) {v.Versenyszam} versenyszámban indul, rajtszáma {v.Rajtszam}. {rekord(v.OR, "Olimpiai csúccsal")}, {rekord(v.VR, "világcsúccsal")}, {rekord(v.ER, "Európa-csúccsal")}.";
    }
    private void Hiba(string uzenet) => MessageBox.Show(this, uzenet, "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
}
