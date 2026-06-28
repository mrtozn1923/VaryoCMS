# Global Claude Preferences

## Dil
- Bana her zaman **Türkçe** yanıt ver
- Code, comments, variable names, SQL: **English**

## İletişim Tarzı
- Mimari veya teknik bir seçim önerirken **2 seçenek + kısa tradeoff** ver
- Bir task tamamlandığında **"Sonraki adım: [ne]"** de
- Sormadığım kodu refactor etme, sadece söyle
- Eğer "yap" veya "ekle" diyip detay vermezsem **1 soru sor**, tahmin etme

## Genel Kodlama Tercihleri
- `async/await` her zaman — `.Result` veya `.Wait()` asla
- Constructor injection — service locator asla
- Explicit type kullan, `var`'ı her yere koyma
- Unit test istemediğim sürece yazma

## macOS Ortamı
- Terminal komutları macOS/zsh için ver
- Path önerilerinde `~` kullan
