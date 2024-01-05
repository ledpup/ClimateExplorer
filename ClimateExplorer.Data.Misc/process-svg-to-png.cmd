md 128

for %%d in (*.svg) do "C:\Program Files\Inkscape\bin\inkscape.com" --export-type="png" --export-width=128 %%d --export-filename "128\%%~nd.png"

md 256

for %%d in (*.svg) do "C:\Program Files\Inkscape\bin\inkscape.com" --export-type="png" --export-width=256 %%d --export-filename "256\%%~nd.png"


md 512

for %%d in (*.svg) do "C:\Program Files\Inkscape\bin\inkscape.com" --export-type="png" --export-width=512 %%d --export-filename "512\%%~nd.png"


md 1024

for %%d in (*.svg) do "C:\Program Files\Inkscape\bin\inkscape.com" --export-type="png" --export-width=1024 %%d --export-filename "1024\%%~nd.png"