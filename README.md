# AudioStream

AudioStream capture le son du PC Windows et le diffuse sur le reseau local via une page web.

## Lancer

```powershell
dotnet run --project src\AudioStream -- --port 5100
```

La console affiche ensuite une URL locale et une ou plusieurs URLs LAN, par exemple:

```text
URL locale: http://localhost:5100
URL LAN:    http://192.168.1.33:5100
```

Ouvre l'URL LAN depuis un telephone, une tablette ou un autre ordinateur connecte au meme reseau local, puis clique sur `Demarrer l'ecoute`.

Au demarrage, AudioStream affiche une banniere coloree et verifie si le port TCP choisi est autorise dans le pare-feu Windows. Si le port n'est pas autorise, la console propose de creer automatiquement la regle entrante.

## Options

```powershell
dotnet run --project src\AudioStream -- --host 0.0.0.0 --port 5100 --sample-rate 48000
```

- `--host`: interface d'ecoute, par defaut `0.0.0.0`.
- `--port`: port HTTP, par defaut `5100`.
- `--sample-rate`: frequence envoyee au navigateur, par defaut `48000`.

## Verifier

```powershell
dotnet test AudioStream.sln
```

Si un appareil du reseau local ne se connecte pas, autorise l'application dans le pare-feu Windows au premier lancement.
