# Архив без .git и .gitignore-мусора

Самый нормальный способ из репы:

`git archive --format=zip --output=VYRA.zip HEAD`

Он берёт только tracked files. То есть без `.git`, без `bin/`, `obj/`, без всего, что не закоммичено.

Если надо из текущего состояния, включая незакоммиченное:

`git ls-files -co --exclude-standard | zip VYRA.zip -@`

Вот это прям “архив рабочего дерева, но без игнорируемого мусора”.
