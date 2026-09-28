# Guia do LifeHacks: Trackers Customizados, Central de Compras & APK Android

Este documento explica como utilizar os novos recursos implementados e como gerar o seu arquivo **APK instalável** para o smartphone Android.

---

## 1. Trackers Quantitativos Dinâmicos (Adeus ao Hardcoded!)
A aplicação agora não está mais presa a uma única meta de água. Qualquer hábito ou ingestão quantitativa pode ser monitorada:

- **Como Funciona**:
  - Cada tracker possui seu próprio nome, unidade (`ml`, `xícaras`, `latas`, `copos`, `doses`, `páginas`), quantidade da dose, meta diária, ícone e cor de destaque exclusiva.
  - O app já vem com **Água** pré-configurada (500ml x 4 = 2000ml), mas agora ela é **100% editável**.
  - Você pode clicar em **"+ Novo Tracker"** na aba **Hoje** e criar:
    - 🍵 **Chá Verde**: 200ml por dose, meta de 600ml (3 xícaras).
    - 🥤 **Refrigerante Zero**: 350ml por lata, meta limite de 700ml (2 latas).
    - ☕ **Café**: 100ml por xícara, meta de 300ml.
    - 📖 **Leitura Diária**: 5 páginas por dose, meta de 20 páginas.
    - 🥛 **Whey Protein**: 1 dose de 30g.
- **Lembretes Personalizados por Tracker**:
  - Cada tracker pode ter sua própria janela de horário (ex: Chá das 14:00 às 18:00) e intervalo de minutos para lembrete.

---

## 2. Central de Compras, Wishlist & Diário de Pesquisa (Hardware & Virada do Cartão)
A aba **Pendências** foi completamente promovida para uma **Central de Compras & Wishlist**:

- **Planejamento Financeiro da Virada do Cartão**:
  - O topo da tela calcula em tempo real o **Total Previsto para a Virada do Cartão** somando os valores em R$ de tudo o que você marcou para comprar no próximo fechamento de fatura.
- **Captura Ágil em 1 Segundo**:
  - Digite o nome do produto no campo de texto e pressione **Enter**. O item é adicionado instantaneamente.
- **Diário de Pesquisa e Impressões**:
  - Clique em qualquer card (como *"Placa de Vídeo RTX 4070 Ti Super"*) para abrir a gaveta de detalhes.
  - **Feed de Anotações Cronológicas**: Anote tudo o que você achou de relevante nos testes (ex: *"Review do Gamers Nexus mostrou que o modelo ASUS Dual tem VRM 15°C mais frio que o concorrente"*).
  - **Links Diretos de Reviews e Ofertas**: Adicione URLs de canais de benchmark, comparadores de preço e links da KaBuM/Terabyte. Cada link tem um botão para abrir diretamente no navegador (`↗`).
- **Prioridades e Filtros**:
  - Filtre por *"Virada do Cartão"*, *"Todos os Itens"*, *"Alta Prioridade"* ou *"Comprados"*.

---

## 3. Como Gerar e Instalar o APK no seu Celular Android

### Via PWABuilder (Recomendado - 2 Minutos, Sem Instalar SDKs no PC)
A Microsoft mantém a ferramenta oficial e gratuita [PWABuilder](https://www.pwabuilder.com), que empacota aplicações WebAssembly Standalone em arquivos `.apk` prontos e assinados:

1. O app já está publicado automaticamente no GitHub Pages via workflow de deploy.
2. Acesse [PWABuilder.com](https://www.pwabuilder.com) e cole a URL da sua aplicação.
3. O PWABuilder validará o manifesto e o service worker que já configuramos.
4. Clique em **"Package for Android"** -> **"Generate APK"**.
5. Baixe o arquivo `.apk`, envie para o seu celular (via WhatsApp, Google Drive ou cabo) e instale diretamente.
