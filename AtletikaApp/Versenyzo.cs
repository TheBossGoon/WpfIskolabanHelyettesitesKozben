namespace AtletikaApp;

public sealed class Versenyzo
{
    public int Rajtszam { get; set; }
    public string Nev { get; set; } = "";
    public string Versenyszam { get; set; } = "";
    public string Nem { get; set; } = "";
    public int Eletkor { get; set; }
    public bool OR { get; set; }
    public bool VR { get; set; }
    public bool ER { get; set; }
}
