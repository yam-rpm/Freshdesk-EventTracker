Vorgehen nach der Projekterstellung

1. NuGet-Pakete wiederherstellen
- Stelle sicher, dass alle benötigten Pakete installiert sind (Restore NuGet Packages in Visual Studio oder via CLI).

2. appsettings.json anpassen
- Trage die korrekten Datenbankverbindungsinformationen ein (SqlServerDbConnection).
- Optional: andere Konfigurationen wie DiscriminatorValues prüfen und ggf. anpassen.

3. Projekt kompilieren
- Stelle sicher, dass das Projekt fehlerfrei kompiliert (Build Solution).
  - Ggf. die Projekte in den entsprechenden Projekten neu einbinden

4. Datenbank installieren / Migrationen ausführen
- Migrationen auf den angegebenen SQL-Server anwenden:
  Update-Database -Project <EntityFramework-Projekt> -StartupProject <Starter-Projekt> -Verbose
- Prüfe, dass die Datenbank und Tabellen erstellt wurden.

5. Projekt starten
- Starte das Projekt (Run oder F5).
- Überprüfe, ob die Anwendung erfolgreich auf die Datenbank zugreifen kann.

6. Logs
- Logs werden standardmäßig über NLog in <Dokumente>\RpmInformatik\{projektname}\logs geschrieben.