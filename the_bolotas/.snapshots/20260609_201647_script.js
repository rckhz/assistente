// Animação dos números
function animarNumero(id, alvo, duracao = 2000) {
  const el = document.getElementById(id);
  if (!el) return;
  const inicio = 0;
  const incremento = alvo / (duracao / 16);
  let atual = inicio;

  const timer = setInterval(() => {
    atual += incremento;
    if (atual >= alvo) {
      atual = alvo;
      clearInterval(timer);
    }
    el.textContent = Math.floor(atual) + '%';
  }, 16);
}

// Observa quando a seção de dados aparece
const observer = new IntersectionObserver((entries) => {
  entries.forEach(entry => {
    if (entry.isIntersecting) {
      animarNumero('num1', 77);
      animarNumero('num2', 39);
      animarNumero('num3', 67);
      observer.disconnect();
    }
  });
}, { threshold: 0.3 });

const secaoDados = document.querySelector('.dados');
if (secaoDados) observer.observe(secaoDados);

// Botão de compromisso
const btn = document.getElementById('btnCompromisso');
const msg = document.getElementById('mensagem');
let contador = 0;

if (btn) {
  btn.addEventListener('click', () => {
    contador++;
    const frases = [
      '✊ Compromisso firmado. Antirracismo é ação diária.',
      '✊ Mais uma vez! Educação é a chave.',
      '✊ Continue. A luta é de todos os dias.',
      '✊ Forte! Espalhe essa mensagem.'
    ];
    msg.textContent = frases[Math.min(contador - 1, frases.length - 1)];
    btn.style.background = '#27ae60';
    setTimeout(() => btn.style.background = '', 600);
  });
}

console.log('Página carregada. Racismo é crime — Lei 7.716/89.');