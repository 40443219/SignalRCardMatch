let Application = PIXI.Application,
    Container = PIXI.Container,
    loader = PIXI.Loader.shared,
    resources = PIXI.Loader.resources,
    Graphics = PIXI.Graphics,
    TextureCache = PIXI.utils.TextureCache,
    Texture = PIXI.Texture,
    Rectangle = PIXI.Rectangle,
    Sprite = PIXI.Sprite,
    Text = PIXI.Text,
    TextStyle = PIXI.TextStyle;

const viewWidth = 800;
const viewHeight = 600;

let ratio = Math.min(window.innerWidth / viewWidth, window.innerHeight / viewHeight);
let lastViewWidth = viewWidth * ratio;
let lastViewHeight = viewHeight * ratio;

let app = new Application({
    width: viewWidth * ratio,
    height: viewHeight * ratio,
    antialiasing: true,
    transparent: false,
    resolution: 1
});

let cards = [];

app.playerCount = 0;
app.eventQueue = [];
app.lang = 'zh_tw';
app.text = {
    'zh_tw': {
        'menu_btnStart': '開始遊戲'
    }
}

document.body.appendChild(app.view);

loader.add('images/cards.png').load(setup);

let connection = new signalR.HubConnectionBuilder()
                            .withUrl('/gameHub')
                            .build();

connection.on('Test', (msg) => {
    console.log(msg);
});

connection.on('Response', (resJsonString) => {
    console.log(resJsonString);
    var resJson = JSON.parse(resJsonString);
    app.eventQueue.push(resJson);
});

// connection.on('InitializeGame', () => {
//     app.eventQueue.push({target: '', event: 'initializeCards'});
// });

// connection.on('ShowCard', (card) => {
//     console.log(card);
//     const target = app.stage.getChildAt(card.detail["x"] * 4 + card.detail["y"]);
//     app.eventQueue.push({target, event: 'Show', value: card.value});
// });

// connection.on('ShowCardThenHide', (card, lastCard) => {
//     console.log(card, lastCard);
//     const target = app.stage.getChildAt(card.detail["x"] * 4 + card.detail["y"]);
//     const lastTarget = app.stage.getChildAt(lastCard.detail["x"] * 4 + lastCard.detail["y"]);
//     app.eventQueue.push({target, event: 'Show', value: card.value});
//     app.eventQueue.push({target, event: 'Hide', value: card.value});
//     app.eventQueue.push({target: lastTarget, event: 'Hide', value: card.value});
// });

function setup() {
    initialCustomTextures();

    this.onResize();
    window.onresize = onResize;
    window.onreadystatechange = onResize;

    // const card = new Card();
    // card.scale.set(ratio);
    // app.stage.addChild(card);

    // const button = new Button();
    // button.onClick = function() {
    //     console.log(this);
    //     app.eventQueue.push({target: 'playing', event: 'changeStage'});
    // }
    // app.stage.addChild(button);

    initialMenuStage();

    app.ticker.add((delta) => gameLoop(delta));
}

function initialMenuStage() {
    app.status = 'Menu';

    const menu_title = new Banner('Card Match', {
        fill: 'red',
        fontSize: app.screen.height / 12
    });
    menu_title.x = (app.screen.width - menu_title.width) / 2;
    menu_title.y = 0;
    app.stage.addChildAt(menu_title, 0);
    gsap.to(menu_title, 1, { y: app.screen.height / 12 * 3 - menu_title.height / 2 });

    const menu_btnStart = new Button(app.text[app.lang]['menu_btnStart']);
    menu_btnStart.x = (app.screen.width - menu_btnStart.width) / 2;
    menu_btnStart.y = app.screen.height;
    menu_btnStart.onClick = function() {
        app.eventQueue.push({target: 'playing', event: 'changeStage'});
    }
    app.stage.addChildAt(menu_btnStart, 1);
    gsap.to(menu_btnStart, 1.25, { y: app.screen.height / 12 * 7 - menu_btnStart.height / 2 });
}

function initialGameStage() {
    app.status = 'Requesting for a new Game';

    connection.start()
                .then(() => {
                    // connection.invoke('Test'); 
                    // connection.invoke('NewGame');
                    connection.invoke('Request',{ 
                        event: 'NewGame'
                    });
                })
                .catch((err) => console.error(err));
}

function initializeCards() {
    app.status = 'Initializing cards';

    app.stage.interactiveChildren = false;

    const spacingX = 25 * ratio;
    const spacingY = 10 * ratio;
    for(let i = 0; i < 4; i++) {
        for(let j = 0; j < 4; j++) {
            const card = new Card();
            card.SetPos((card.width + spacingX) * i , (card.height + spacingY) * j);
            card.detail.y = i;
            card.detail.x = j;
            card.onClick = function() {
                // connection.invoke('CardClicked', card.detail);
                connection.invoke('Request', {
                    event: 'CardClicked',
                    options: {
                        cardDetail: card.detail
                    }
                });
            };
            card.Enable();
            app.stage.addChildAt(card, i * 4 + j);
        }
    }

    app.banner = new Banner('Your turn', { 'fill': 'white' });
    app.banner.y = app.screen.height - app.banner.height * 2;
    app.stage.addChild(app.banner);

    app.stage.interactiveChildren = true;
}

function changeStage(nextStage, preStage) {
    app.status = 'Changing stage';
    
    switch(nextStage) {
        case "playing":
            clearStage();
            initialGameStage();
            break;
    }
}

function clearStage() {
    while(app.stage.children.length > 0) {
        app.stage.removeChildAt(0);
    }
}

function gameLoop(delta) {
    if(app.status === 'waiting') {
        console.log('waitting');
        return;
    }
    while(app.eventQueue.length > 0) {
        const e = app.eventQueue[0];
        if((typeof e.target) === 'string') {
            if(e.event === 'changeStage') {
                changeStage(e.target);
            } else if(e.event === 'AITurn') {
                setTimeout(() => {
                    app.stage.interactiveChildren = false;
                    app.banner.text = e.options.banner;
                }, 500);
            } else if(e.event === 'userTurn') {
                app.stage.interactiveChildren = false;
                setTimeout(() => {
                    app.stage.interactiveChildren = true;
                    app.banner.text = e.options.banner;
                }, 500);
            } else if(e.event === 'end') {
                app.stage.interactiveChildren = false;
                app.banner.text = e.options.banner;
            } else if (e.event === 'InitializeCards') {
                initializeCards();
            } else if (e.event === 'ShowCards') {
                app.stage.interactiveChildren = false;
                app.status = 'waiting';

                for(const card of e.options.cards) {
                    const _card = app.stage.getChildAt(card.detail.y * 4 + card.detail.x);
                    _card.CardFront = cards[card.value];
                    _card.Show();
                    _card.Disable();
                    const audio = new Audio('lib/js/test.mp3');
                    audio.play();
                }
                if(e.options.banner) {
                    app.banner.text = e.options.banner;
                }

                const chk = setInterval(() => {
                    app.status = 'ShowCards end';
                    connection.invoke('Request', {
                        event: 'ShowCardsEnd'
                    });
                    app.stage.interactiveChildren = true;
                    clearInterval(chk);
                }, 250);
            } else if(e.event === 'ShowCardsThenHide'){
                app.status = 'waiting';
                for(const card of e.options.cards) {
                    const _card = app.stage.getChildAt(card.detail.y * 4 + card.detail.x);
                    _card.CardFront = cards[card.value];
                    _card.Show();
                    _card.Disable();
                    const audio = new Audio('lib/js/test.mp3');
                    audio.play();
                }
                if(e.options.banner) {
                    app.banner.text = e.options.banner;
                }

                const chk = setInterval(() => {
                    app.status = 'ShowCards end';
                    for(const card of e.options.cards) {
                        const _card = app.stage.getChildAt(card.detail.y * 4 + card.detail.x);
                        _card.Cover();
                        _card.Enable();
                        const audio = new Audio('lib/js/test.mp3');
                        audio.play();
                    }
                    app.status = 'waiting';
                    setTimeout(() => {
                        app.status = 'CoverCard end';
                        // app.status = 'waiting';
                        connection.invoke('Request', {
                            event: 'ShowCardsEnd'
                        });
                        app.stage.interactiveChildren = true;
                    }, 250);
                    clearInterval(chk);
                }, 500);
            }
        } else if(e.target instanceof Button) {
            const _button = e.target;
            switch(e.event) {
                case 'pointerdown':
                    _button.onBtnDown();

                    break;
                case 'pointerup':
                    _button.onBtnUp();

                    break;
                case 'pointerupoutside':
                    _button.onBtnUp();

                    break;
                case 'pointerover':
                    _button.onHover();

                    break;
                case 'pointerout':
                    _button.onHoverOut();

                    break;
                default:

                    break;
            }
        } else if(e.target instanceof Card) {
            const _card = e.target;
            switch(e.event) {
                case 'pointerdown':
                    _card.onBtnDown();

                    break;
                case 'pointerup':
                    _card.onBtnUp();

                    break;
                case 'pointerupoutside':
                    _card.onBtnUp();

                    break;
                case 'pointerover':
                    _card.onHover();

                    break;
                case 'pointerout':
                    _card.onHoverOut();

                    break;
                // case 'Show':
                //     if(e.value !== undefined || e.value !== null) {
                //         _card.CardFront = cards[e.value];
                //     }
                //     _card.Show();
                //     _card.Disable();
                //     e.status = 'done';
                //     break;
                // case 'Hide':
                //     app.stage.interactiveChildren = false;

                //     setTimeout(() => {
                //         _card.Cover();
                //         _card.Enable();
                //     }, 1000);

                //     setTimeout(() => {
                //         app.stage.interactiveChildren = true;
                //     }, 1000);
                //     e.status = 'done';
                //     break;
                default:

                    break;
            }
        }
        
        app.eventQueue.shift();
    }
}

function end() {

}

function initialCustomTextures() {
    // ---------- Cards ----------
    const spacingX = 1;
    const spacingY = 1;
    const cardWidth = 48;
    const cardHeight = 72;
    const texture = TextureCache['images/cards.png'];
    // 1-13
    for(let i = 0; i < 4; i++) {
        for(let j = 0; j < 13; j++) {
            const rectangle = new Rectangle((cardWidth + spacingX) * j, (cardHeight + spacingY) * i, cardWidth, cardHeight);
            const card = new Texture(texture, rectangle);

            cards.push(card);
        }
    }
    // Two covers
    cards.push(new Texture(texture, new Rectangle((cardWidth + spacingX) * 0, (cardHeight + spacingY) * 4, cardWidth, cardHeight)));
    cards.push(new Texture(texture, new Rectangle((cardWidth + spacingX) * 1, (cardHeight + spacingY) * 4, cardWidth, cardHeight)));
    
    // Joker
    cards.push(new Texture(texture, new Rectangle((cardWidth + spacingX) * 2, (cardHeight + spacingY) * 4, cardWidth, cardHeight)));
}

function onResize() { 
    let w = window.innerWidth; 
    let h = window.innerHeight; 
    ratio = Math.min(w / viewWidth, h / viewHeight); 
    app.view.style.marginLeft = (w - ratio * viewWidth) / 2 + "px"; 
    app.view.style.marginTop = (h - ratio * viewHeight) / 2 + "px"; 
    app.renderer.resize(ratio * viewWidth,ratio * viewHeight);

    for(const item of app.stage.children) {
        try {
            item.Repaint();
        } catch(err) {
            console.error(err);
        }
    }

    lastViewWidth = app.renderer.width;
    lastViewHeight = app.renderer.height;
}

class Banner extends Text {
    constructor(_title, _style) {
        super();

        this.text = _title || '';
        this.style = _style || {};
    }

    setPos(x, y) {
        this.x = x / lastViewWidth * app.renderer.width;
        this.y = y / lastViewHeight * app.renderer.height;
    }

    Repaint() {
        this.setPos(this.x, this.y);
        this.style.fontSize = this.style.fontSize / lastViewHeight * app.renderer.height;
    }
}

class Button extends Container {
    constructor(_title) {
        super();

        this.title = _title || '';
        this.BackgroundColor = 0xC0C0C0;
        this.BorderColor = 0x00FFFF;
        this.BackgroundColor_Normal = 0xC0C0C0;
        this.BorderColor_Normal = 0x00FFFF;
        this.BackgroundColor_Hover = 0x00FF00;
        this.BorderColor_Hover = 0x00FFFF;
        this.BackgroundColor_Down = this.BackgroundColor_Normal;
        this.BorderColor_Down = this.BorderColor_Normal;
        this.btnStat = {
            isDown: false,
            isOver: false
        };
        this.interactive = true;
        this.buttonMode = true;

        const self = this;
        this.on('pointerdown', () => app.eventQueue.push({ target: self, event: 'pointerdown' }))
                .on('pointerup', () => app.eventQueue.push({ target: self, event: 'pointerup' }))
                .on('pointerupoutside', () => app.eventQueue.push({ target: self, event: 'pointerupoutside' }))
                .on('pointerover', () => app.eventQueue.push({ target: self, event: 'pointerover' }))
                .on('pointerout', () => app.eventQueue.push({ target: self, event: 'pointerout' }));

        this.DrawShape();
        this.DrawTitle();
    }

    onClick() {}

    onBtnDown() {
        this.btnStat.isDown = true;
        this.onClick();
        this.Repaint();
    }

    onBtnUp() {
        this.btnStat.isDown = false;
        this.Repaint();
    }

    onHover() {
        this.btnStat.isOver = true;
        this.Repaint();
    }

    onHoverOut() {
        this.btnStat.isOver = false;
        this.Repaint();
    }

    DrawTitle() {
        if(this.children.length > 1) {
            this.removeChildAt(1);
        }

        const _title = new Text(this.title, {
            fill: 'black',
            fontSize: this.height / 2
        });
        _title.anchor.set(0.5);
        _title.x = this.width / 2;
        _title.y = this.height / 2;
        this.addChildAt(_title, 1);
    }

    DrawShape() {
        if(this.children.length > 0) {
            this.removeChildAt(0);
        }

        this.BackgroundColor = (this.btnStat.isDown) ? this.BackgroundColor_Down : ((this.btnStat.isOver) ? this.BackgroundColor_Hover : this.BackgroundColor_Normal);
        this.BorderColor = (this.btnStat.isDown) ? this.BorderColor_Down : ((this.btnStat.isOver) ? this.BorderColor_Hover : this.BorderColor_Normal);

        const _button = new Graphics();
        _button.lineStyle(4, this.BorderColor, 1);
        _button.beginFill(this.BackgroundColor);
        _button.drawRect(0, 0, 192 * ratio, 64 * ratio);
        _button.endFill();
        this.addChildAt(_button, 0);
    }

    SetPos(x, y) {
        this.x = x / lastViewWidth * app.renderer.width;
        this.y = y / lastViewHeight * app.renderer.height;
    }

    Repaint() {
        this.DrawShape();
        this.DrawTitle();
        this.SetPos(this.x, this.y);

        // console.log(lastViewWidth, app.view.width, app.renderer.width);
    }
}

class Card extends Sprite {
    constructor(frontTexture, coverTexture) {
        super();
        this.CardCover = coverTexture || cards[52];
        this.CardCover_Hover = coverTexture || cards[53];
        this.CardFront = frontTexture || cards[54];
        this.texture = this.CardCover;
        this.btnStat = {
            isDown: false,
            isOver: false,
            isShown: false
        };
        this.detail = {};

        this.scale.set(ratio);

        const self = this;
        this.on('pointerdown', () => app.eventQueue.push({ target: self, event: 'pointerdown' }))
                .on('pointerup', () => app.eventQueue.push({ target: self, event: 'pointerup' }))
                .on('pointerupoutside', () => app.eventQueue.push({ target: self, event: 'pointerupoutside' }))
                .on('pointerover', () => app.eventQueue.push({ target: self, event: 'pointerover' }))
                .on('pointerout', () => app.eventQueue.push({ target: self, event: 'pointerout' }));
    }

    SetPos(x, y){
        this.x = x / lastViewWidth * app.renderer.width;
        this.y = y / lastViewHeight * app.renderer.height;
    }

    Enable() {
        this.interactive = true;
        this.buttonMode = true;
    }

    Disable() {
        this.interactive = false;
        this.buttonMode = false;
    }

    onClick() {}

    onBtnDown() {
        this.btnStat.isDown = true;
        this.onClick();
        this.Repaint();
    }

    onBtnUp() {
        this.btnStat.isDown = false;
        this.Repaint();
    }

    onHover() {
        this.btnStat.isOver = true;
        this.Repaint();
    }

    onHoverOut() {
        this.btnStat.isOver = false;
        this.Repaint();
    }

    Show() {
        this.btnStat.isShown = true;
        this.Repaint();
    }

    Cover() {
        this.btnStat.isShown = false;
        this.Repaint();
    }

    Repaint() {
        this.texture = (this.btnStat.isShown) ? this.CardFront : ((this.btnStat.isOver) ? this.CardCover_Hover : this.CardCover );
        this.scale.set(ratio);
        this.SetPos(this.x, this.y);
    }
}