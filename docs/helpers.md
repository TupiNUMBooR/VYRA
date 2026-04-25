# Assets

```sh
cd "VYRA.WPF/Assets"

mogrify -format jpg *.png

magick "VYRA1.jpg" \
    -background none \
    -filter Lanczos \
    \( +clone -resize 256x256 -gravity center -extent 256x256 \) \
    \( +clone -resize 128x128 -gravity center -extent 128x128 \) \
    \( +clone -resize 64x64  -gravity center -extent 64x64  \) \
    \( +clone -resize 48x48  -gravity center -extent 48x48  \) \
    \( +clone -resize 32x32  -gravity center -extent 32x32  \) \
    \( +clone -resize 24x24  -gravity center -extent 24x24  \) \
    \( +clone -resize 20x20  -gravity center -extent 20x20  \) \
    \( +clone -resize 16x16  -gravity center -extent 16x16  \) \
    -delete 0 \
    "VYRA1.ico"
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
