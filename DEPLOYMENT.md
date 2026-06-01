# Deployment

Das Image wird von der CI nach `ghcr.io/daniel-vetter/habittracker:latest` gepusht.

## Container starten

```sh
docker run -d --name habittracker --restart unless-stopped \
  -p 8080:80 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -e "ConnectionStrings__db=Host=...;Database=habittracker;Username=...;Password=..." \
  ghcr.io/daniel-vetter/habittracker:latest
```

## Self-Update

Die System-Seite (`/system`) zeigt Build-Infos und steuert das Self-Update.

Damit der Update-Mechanismus aktiv ist, muss der Container:

- als **echter Docker-Container** laufen (Erkennung über `/.dockerenv`),
- den **Docker-Socket** gemountet haben (`-v /var/run/docker.sock:/var/run/docker.sock`),
- einen **festen Namen** haben (`--name habittracker`) und idealerweise eine **Restart-Policy**.

Ist eine dieser Bedingungen nicht erfüllt (z. B. lokal über Aspire), deaktiviert sich das Feature
selbst und die Seite zeigt nur die Build-/Systeminfos.

Ablauf eines Updates: Die App vergleicht stündlich (oder per „Jetzt prüfen") die Image-ID des
laufenden Containers mit der frisch gepullten `:latest`-ID. Bei „Jetzt installieren" (oder mit
aktiviertem Auto-Update) startet sie einen kurzlebigen `docker:cli`-Sidecar-Container, der den
App-Container stoppt, entfernt und mit dem neuen Image und identischer Konfiguration (Ports, Volumes,
Env, Restart-Policy, Netzwerk) neu erzeugt. Nach dem Neustart räumt die App den Sidecar weg und
schreibt dessen Logs in die `UpdateLogs`-Tabelle (sichtbar unter „Update-Logs").

> Hinweis: Anwendungsdaten müssen über Volumes persistiert werden, da der Container neu erstellt wird.
