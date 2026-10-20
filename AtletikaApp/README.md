# Atlétikai rajtszámkezelő

Windows WPF alkalmazás a DataGrid kiegészítő feladat követelményeivel. A felület üres táblázattal és az „Összes” szűrővel indul.

## Indítás

Windows rendszeren, .NET 8 SDK-val nyissa meg az `AtletikaApp.csproj` fájlt Visual Studio-ban, vagy futtassa a projekt mappájából:

```powershell
dotnet run
```

## CSV-formátum

A betöltés és a CSV-mentés pontos fejlécsora:

```text
Rajtszam;Nev;Versenyszam;Nem;Eletkor;OR;VR;ER
```

Az OR, VR és ER mezők az olimpiai csúcsot, világcsúcsot és Európa-csúcsot jelölik (0 vagy 1). A mellékelt `atletika_minta.csv` ehelyett a korábbi feladat világbajnoki és Európa-bajnoki csúcsmezőit tartalmazza. Ezek jelentése eltér, ezért az új alkalmazás bemeneti fájljaként nem szerepel automatikusan.
