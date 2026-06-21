# keylogger nao. exemplo simples: contador de teclas (sem capturar conteudo).
# roda no terminal, mostra quantas teclas voce apertou ate dar Ctrl+C.

from pynput import keyboard

contador = 0

def on_press(key):
    global contador
    contador += 1
    print(f"teclas pressionadas: {contador}", end="\r")

print("contando teclas. Ctrl+C pra sair.")
with keyboard.Listener(on_press=on_press) as l:
    l.join()
