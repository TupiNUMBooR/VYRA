# Assets

```sh
cd "VYRA.WPF/Assets"

mogrify -format jpg *.png

magick -background none \
    -filter Lanczos \
    \( VYRA3.png -resize 256x256 -gravity center -extent 256x256 \) \
    \( VYRA3.png -resize 128x128 -gravity center -extent 128x128 \) \
    \( VYRA3.png -resize 64x64  -gravity center -extent 64x64  \) \
    \( VYRA3.png -resize 48x48  -gravity center -extent 48x48  \) \
    \( VYRA8.png -resize 32x32  -gravity center -extent 32x32  \) \
    \( VYRA8.png -resize 24x24  -gravity center -extent 24x24  \) \
    \( VYRA8.png -resize 20x20  -gravity center -extent 20x20  \) \
    \( VYRA8.png -resize 16x16  -gravity center -extent 16x16  \) \
    -delete 0 \
    "icon.ico"

magick VYRA1.jpg \
  -background black \
  -resize 500x500 \
  -gravity center \
  -extent 630x500 \
  icon-itch.jpg
```

# Архив без .git и .gitignore-мусора

Самый нормальный способ из репы:

`git archive --format=zip --output=VYRA.zip HEAD`

Он берёт только tracked files. То есть без `.git`, без `bin/`, `obj/`, без всего, что не закоммичено.

Если надо из текущего состояния, включая незакоммиченное:

`git ls-files -co --exclude-standard | zip VYRA.zip -@`

Вот это прям “архив рабочего дерева, но без игнорируемого мусора”.

# Project

```ps1
dotnet build
dotnet run --project "VYRA.WPF/VYRA.WPF.csproj"
```
