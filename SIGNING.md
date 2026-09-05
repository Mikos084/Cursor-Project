# Podpisywanie wersji release

Multiple Pointers nie próbuje omijać SmartScreen ani antywirusów.

Dla publicznie dystrybuowanej aplikacji Windows najbardziej sensowna droga to:

1. zbudować Release,
2. uzyskać zaufany certyfikat code-signing / usługę Artifact Signing,
3. podpisać EXE i pozostałe własne binaria,
4. nie modyfikować plików po podpisaniu,
5. używać tej samej tożsamości wydawcy w kolejnych wydaniach.

Przykładowy schemat z Windows SDK `signtool`:

```powershell
signtool sign /fd SHA256 /tr <timestamp-server> /td SHA256 /a MultiplePointers.exe
```

Nie wstawiono tu konkretnego certyfikatu ani poświadczeń — podpis powinien należeć do faktycznego wydawcy.
