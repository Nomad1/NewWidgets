/* runmobile_design.js -- browser-preview tint. See runmobile_design.md for what this file is allowed to do.
 * The engine draws a frame as texture(...) * Color, a per-channel multiply; an SVG
 * feColorMatrix with a diagonal of R,G,B (tint channel / 255) and identity alpha
 * reproduces that exactly (measured max error < 1 level of 255).
 */
(function () {
    'use strict';

    var FRAME_PROPS = [
        'position', 'left', 'top', 'width', 'height', 'margin',
        'boxSizing', 'borderStyle', 'borderWidth', 'borderColor',
        'borderImageSource', 'borderImageSlice', 'borderImageWidth',
        'borderImageOutset', 'borderImageRepeat',
        'backgroundImage', 'backgroundSize', 'backgroundPosition',
        'backgroundRepeat', 'backgroundClip', 'clipPath'
    ];

    // ui.css's only tint forms: a 6-digit hex or rgba()/rgb(). `transparent`
    // (never a real tint) resolves to null, same as any unparsed value.
    function parseColor(text) {
        text = text.trim();
        if (!text) return null;
        var m = /^#([0-9a-f]{6})$/i.exec(text);
        if (m) {
            return [m[1].slice(0, 2), m[1].slice(2, 4), m[1].slice(4, 6)].map(function (h) { return parseInt(h, 16); });
        }
        m = /^rgba?\(\s*([\d.]+)[,\s]+([\d.]+)[,\s]+([\d.]+)/i.exec(text);
        if (m) return [parseFloat(m[1]), parseFloat(m[2]), parseFloat(m[3])];
        return null;
    }

    // ui.css's only form for --background-opacity: a percentage.
    function parseOpacity(text) {
        var m = /^([\d.]+)%$/.exec((text || '').trim());
        return m ? parseFloat(m[1]) / 100 : null;
    }

    // Reads a custom property on `el` only if `el` itself declares it -- it
    // inherits by default, and ui.css sets `--background-color` once on
    // <html>, so comparing to the parent's computed value tells inherited
    // apart from own. ponytail: a coincidental match isn't handled -- doesn't occur in ui.css.
    function ownValue(el, parent, prop) {
        var value = getComputedStyle(el).getPropertyValue(prop).trim();
        if (!value) return null;
        if (parent && value === getComputedStyle(parent).getPropertyValue(prop).trim()) return null;
        return value;
    }

    function toHex2(n) {
        n = Math.max(0, Math.min(255, Math.round(n)));
        return n.toString(16).padStart(2, '0');
    }

    var defsSvg = null;

    // Builds (once per colour) the <filter> reproducing the tint multiply --
    // one per colour, not shared, because a shared <filter> can't read
    // var(--background-color) from the widget; it resolves var() at its own tree position.
    function ensureFilter(rgb) {
        var id = 'nw-tint-' + rgb.map(toHex2).join('');
        if (document.getElementById(id)) return id;
        if (!defsSvg) {
            defsSvg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            defsSvg.id = 'nw-tint-defs';
            defsSvg.setAttribute('width', '0');
            defsSvg.setAttribute('height', '0');
            defsSvg.style.position = 'absolute';
            document.body.appendChild(defsSvg);
        }
        var NS = 'http://www.w3.org/2000/svg';
        var filter = document.createElementNS(NS, 'filter');
        filter.id = id;
        filter.setAttribute('color-interpolation-filters', 'sRGB'); // mandatory: default linearRGB puts blue ~76 levels out
        var matrix = document.createElementNS(NS, 'feColorMatrix');
        matrix.setAttribute('type', 'matrix');
        var r = rgb[0] / 255, g = rgb[1] / 255, b = rgb[2] / 255;
        matrix.setAttribute('values',
            r.toFixed(6) + ' 0 0 0 0  0 ' + g.toFixed(6) + ' 0 0 0  0 0 ' + b.toFixed(6) + ' 0 0  0 0 0 1 0');
        filter.appendChild(matrix);
        defsSvg.appendChild(filter);
        return id;
    }

    // `filter` applies to the element's WHOLE SUBTREE (measured: white text
    // rendered 252,234,137 under a tint filter on a parent) and makes the
    // element a containing block for absolute descendants (measured: a child
    // moved from left 0 to 44) -- so it can't sit on the widget itself. Hence a
    // separate clone draws the filtered frame; the widget stays unfiltered on top.
    //
    // The clone is inserted as el's own previous sibling in el's EXISTING parent --
    // el is never moved. An earlier version wrapped [clone, el] in a `display:contents`
    // container and reparented el into it, which achieves the same paint order (a
    // DOM-earlier sibling paints behind, same stacking context, no z-index needed) but
    // moving an already-connected node is a remove-then-reinsert: it drops focus (a
    // just-focused element was blurred by the very code meant to render its focused
    // appearance -- the close button's :focus never rendered) and, measured the same way,
    // left :hover's/:focus's own border-image/background-image unable to paint at all
    // even once suppressed on el correctly. Plain sibling insertion needs no wrapper,
    // moves nothing, and both symptoms are gone with it -- the move was never load-bearing,
    // only the paint order was, and a DOM-earlier sibling gets that for free.
    var CLONES = new WeakMap(); // el -> { clone }, created lazily

    function getOrCreateClone(el) {
        var rec = CLONES.get(el);
        if (rec) return rec;

        var clone = document.createElement('div');
        clone.setAttribute('aria-hidden', 'true');
        clone.style.pointerEvents = 'none';
        clone.style.display = 'none'; // shown by sync() once it has a real tint

        el.parentNode.insertBefore(clone, el); // paints behind el; el itself never moves

        rec = { clone: clone };
        CLONES.set(el, rec);
        return rec;
    }

    // Re-reads el's tint/frame and updates its clone -- or restores el's own
    // frame and hides the clone if untinted; runs at load and on every state-changing event.
    function sync(el) {
        // CHECKBOX_STRUCT.has(el): row 5 owns that <input>'s opacity; label-hover's forwarded :hover otherwise matches `input:hover{--background-color}` and gets it tinted here.
        if (!el || el.id === 'nw-tint-defs' || CHECKBOX_STRUCT.has(el)) return;
        if (el === document.body || el === document.documentElement) return;

        var parent = el.parentElement;
        var colorText = ownValue(el, parent, '--background-color');
        var rgb = colorText ? parseColor(colorText) : null;
        var opacityText = ownValue(el, parent, '--background-opacity');
        var opacity = opacityText ? parseOpacity(opacityText) : null;

        var rec = CLONES.get(el);

        if (!rgb && opacity === null) {
            // Neither now: if el was tinted/dimmed before, restore its own frame
            // (drop the inline override, let ui.css's cascade draw it again) and hide the clone.
            // Only touch opacity when this mechanism owns it (rec existing means it does) --
            // an unconditional write here clobbered the inline opacity:0 row 5 uses to hide
            // the real checkbox <input>, the moment any event handler called sync() on it
            // (measured: the frame that leaked in was that input's own border-image,
            // uncovered once its opacity reverted to 1).
            if (rec) {
                rec.clone.style.display = 'none';
                el.style.borderImageSource = '';
                el.style.backgroundImage = '';
                el.style.backgroundColor = '';
                el.style.opacity = '';
            }
            return;
        }

        // Both --background-color and --background-opacity are frame-only in the engine --
        // WidgetButton's m_image/m_label and WidgetCheckBox's tick are separate widgets the
        // button/checkbox never dims or tints through its own back_style/back_opacity. CSS
        // `opacity` has no such split: it dims the whole rendered subtree, so setting it on
        // el directly used to dim real children too (measured: .square_button's icons came
        // out at its frame's own 30% back_opacity). --background-opacity ALONE (no tint
        // colour -- .square_button's resting state, .back_pattern) therefore goes through
        // the same clone as a real tint, just with no SVG filter on it.
        rec = getOrCreateClone(el);

        // A previous sync() may have suppressed el's own frame inline, which
        // would poison the read below (getComputedStyle sees the inline value,
        // not :focus/:hover/:checked). Lift it first so the clone gets el's current frame.
        el.style.borderImageSource = '';
        el.style.backgroundImage = '';
        var computed = getComputedStyle(el);
        FRAME_PROPS.forEach(function (prop) { rec.clone.style[prop] = computed[prop]; });
        // FRAME_PROPS just copied el's own computed backgroundSize -- but el's own is never
        // what row 8 manages (the copy below suppresses el's backgroundImage, so row 8's
        // parseSpriteRef never matches el again after this point); the clone is the element
        // that actually needs an SVG-fragment sprite's real pixel size. Row 8 already owns
        // that derivation (SPRITE_SIZES, repeat-vs-contain) -- reuse it on the clone here
        // instead of trusting the blind copy above, or the clone keeps whatever bare
        // percentage/auto el's own stylesheet rule declares (measured: back_pattern's tiling
        // rescaled every time this ran, e.g. on a pointerover/pointerout with nothing else
        // changed, because that blind copy overwrote the correct size row 8 had set on THIS
        // SAME clone moments earlier).
        applySpriteSizeTo(rec.clone);

        // FRAME_PROPS copied el's own `position`, and an in-flow el hands the clone `static`
        // or `relative` -- both of which occupy a slot. The clone is el's previous SIBLING, so
        // showing it then inserts a whole extra box into the parent's flow and everything after
        // it shifts (measured: the six .square_buttons in #buttons_panel, a flex row, moved and
        // the row grew the moment one was hovered). It only ever paints, so pin it over el
        // instead. Siblings share an offsetParent, so el's offset box is already in the
        // coordinate space an absolute clone resolves against.
        if (computed.position !== 'absolute' && computed.position !== 'fixed') {
            rec.clone.style.position = 'absolute';
            rec.clone.style.left = el.offsetLeft + 'px';
            rec.clone.style.top = el.offsetTop + 'px';
            rec.clone.style.width = el.offsetWidth + 'px';
            rec.clone.style.height = el.offsetHeight + 'px';
            rec.clone.style.margin = '0'; // the copied margin would offset it a second time
        }

        rec.clone.style.filter = rgb ? 'url(#' + ensureFilter(rgb) + ')' : '';
        rec.clone.style.opacity = opacity !== null ? String(opacity) : '';
        rec.clone.style.display = '';

        // `background-color` is suppressed too because `<input>`'s UA stylesheet
        // paints it opaque white despite `appearance:none` (measured: a hovered text edit rendered flat white).
        el.style.borderImageSource = 'none';
        el.style.backgroundImage = 'none';
        el.style.backgroundColor = 'transparent';
        el.style.opacity = ''; // the clone carries the dim now, not el or its real children
    }

    // A browser gives `<input>` no children, so `.checkbox #checkbox_image`
    // never matches -- build the child the selector expects instead of reading
    // values in script. `mask-image` doesn't resolve `ui.svg#name` (renders nothing), so the tick is a tinted clone, not a mask.
    // Custom tags, not <div>: all three stylesheets carry a bare `panel, div { }` rule
    // (border-image-source, background-size, overflow), and SpaceAdventure's own login.xhtml
    // has no `.checkbox` override for those the way the sample does, so a plain <div> here
    // painted a window-panel frame around the checkbox. A hyphenated tag name is a valid
    // HTML5 custom element -- no user-agent style, matches no bare element rule -- and
    // `.checkbox`/`#checkbox_image` still resolve against it since class/id selectors are
    // tag-agnostic. `box-sizing: border-box` is set explicitly below rather than left to the
    // rule this swap removes, because `.checkbox` itself declares a real `padding` (Test 89,
    // ui.css's own comment) that border-box needs to absorb rather than add to the box.
    var CHECKBOX_STRUCT = new WeakMap();

    function ensureCheckboxStructure(input) {
        var rec = CHECKBOX_STRUCT.get(input);
        if (rec) return rec;

        var computed = getComputedStyle(input);
        var wrapper = document.createElement('checkbox-frame');
        // Kept as a DOM marker only -- syncLabelColor and this function look for it. It used to
        // be the element that DREW the checkbox, back when defaults.css carried a `.checkbox`
        // rule; that rule is gone, because neither a `checkbox` tag nor a `.checkbox` class
        // exists in HTML, and `input[type="checkbox"]` is now the single source of the skin.
        // So the wrapper is a positioning box and nothing else: the input itself draws.
        // NO class is set here. `.checkbox` is Amalthea's own name and this file must work
        // against any project's stylesheet, so the hook is the custom TAG -- <checkbox-frame>
        // is created here and cannot collide with anything a document declares. Contrast
        // .button_image/.button_label/.checkbox_image below, which are hardcoded into the
        // engine's complex widgets and are the same in every project.

        // An unknown element is display: inline, and width/height do nothing on an inline box.
        // The wrapper is given both below, so it has to be a block first. A flex container
        // blockifies its items for free, which is why this only shows outside one.
        wrapper.style.display = 'block';
        wrapper.style.boxSizing = 'border-box';
        ['position', 'left', 'top', 'width', 'height', 'margin'].forEach(function (prop) {
            wrapper.style[prop] = computed[prop];
        });

        // The input and the tick below are `position: absolute; inset: 0`, which resolves
        // against the nearest POSITIONED ancestor -- so the wrapper has to be one, or both of
        // them anchor to the dialog and stay put while the checkbox moves. A checkbox that is
        // itself absolute already qualifies; a `static` one does not, and every checkbox does
        // once its document joins normal flow. `relative` with no offsets moves nothing.
        // Read back from the inline style just written, not getComputedStyle: the wrapper is
        // not in the document until the line below and a detached element computes to nothing.
        if (wrapper.style.position === 'static')
            wrapper.style.position = 'relative';

        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);
        input.style.position = 'absolute';
        input.style.inset = '0';
        input.style.width = '100%';
        input.style.height = '100%';
        input.style.margin = '0';
        // Deliberately NOT hidden. It used to be `opacity: 0` with the wrapper drawing the
        // sprite, which only worked while a `.checkbox` rule existed to draw with. The skin now
        // lives on `input[type="checkbox"]` alone, so hiding the input hides the checkbox: the
        // one element the stylesheet can reach would be the one element nobody can see. The
        // preview sheet's own `appearance: none` stops the browser painting its native control
        // over the sprite, exactly as it does for <button>.

        var tick = document.createElement('checkbox-tick');
        // The class the engine's WidgetCheckBox hardcodes for its tick child, so a project's
        // own `... > .checkbox_image` rule reaches it. It was an id before, which no stylesheet
        // anywhere selects on -- `#checkbox_image` appears in none of them.
        tick.className = 'checkbox_image';
        tick.setAttribute('aria-hidden', 'true');
        tick.style.position = 'absolute';
        tick.style.boxSizing = 'border-box';
        tick.style.inset = '0'; // no bare element rule reaches this tag, so `inset:0` alone sizes it -- no width/height left to force
        // star_ui.svg, not ui.svg: no file named ui.svg exists beside these documents, so this
        // resolved to nothing and the tick never drew. Both CSS references to the same sprite
        // (defaults.css, `checkbox > .checkbox_image` and its :hover) name star_ui.svg.
        tick.style.backgroundImage = 'url("star_ui.svg#check_icon")';
        tick.style.backgroundRepeat = 'no-repeat';
        tick.style.backgroundPosition = 'center';
        tick.style.backgroundSize = 'contain';
        tick.addEventListener('click', function () { input.click(); }); // topmost; forward the click
        wrapper.appendChild(tick);

        rec = { wrapper: wrapper, tick: tick };
        CHECKBOX_STRUCT.set(input, rec);
        return rec;
    }

    // Tints check_icon the same way a widget frame is tinted, shown only when
    // checked (drawn conditionally in code). No :hover branching needed:
    // read straight off the tick's own computed --image-color, which reflects `:hover` natively.
    function syncCheckboxTick(el) {
        // Tag+type, not `.matches('.checkbox')`: simpler, and el keeps that class anyway.
        if (!(el.tagName && el.tagName.toLowerCase() === 'input' && el.type === 'checkbox')) return;
        var rec = ensureCheckboxStructure(el);
        var tick = rec.tick;
        var rgb = parseColor(getComputedStyle(tick).getPropertyValue('--image-color'));
        tick.style.filter = rgb ? 'url(#' + ensureFilter(rgb) + ')' : '';
        tick.style.opacity = el.matches(':checked') ? '1' : '0';
    }

    // Row 11 (runmobile_design.md row 11): a browser gives `<button>` no children, so
    // `button > .button_label` and `.button_steam > .button_image` etc never match --
    // build the children the selectors expect. Supersedes this row's old job of copying a
    // child's padding onto the button itself (there is no bare button text left to
    // approximate the position of once every button has its real label).
    // Custom tags, not <span>: all three stylesheets carry a bare
    // `label, text, span, h1, h2, h3, h4, h5, h6 { }` rule (the sample also `img { }`),
    // which an injected <span> would pick up unwanted style from. A hyphenated tag name is
    // a valid HTML5 custom element -- no user-agent style, matches no bare element rule,
    // and is legal as phrasing content inside <button> (which only forbids interactive
    // content). The class is what the real stylesheet rules match; the tag only keeps
    // element-type rules off it. Never `all: initial`/`unset`: color/font-size/font-family
    // are WidgetParameterInheritance.Inherit (WidgetParameterIndex.cs:203,205), so the
    // engine's own label genuinely inherits them from the button, and resetting here would
    // trade one divergence for another. These two elements exist only at run time -- the
    // engine's markup loader rejects children under <button> (WidgetManager.Markup.cs:597,
    // "Element <button> cannot have children, N node(s) inside it skipped") -- so they are
    // never written into login.xhtml or any other document, only injected here.
    var BUTTON_STRUCT = new WeakMap();

    // The engine positions each child as an inset from the button's own box, not inline
    // flow: WidgetButton.UpdateLayout reads Padding off each child and sizes it as
    // Size - imagePadding.Size. `padding` and `inset` share the same 1-to-4-value
    // top/right/bottom/left syntax, so the child's own computed padding -- read once it is
    // a real child and the real selectors (`.square_button > .button_image`, etc) resolve
    // it -- becomes its inset directly; the padding is zeroed after so it is not also kept
    // as literal box padding on top of the position it just set.
    function positionButtonChild(el) {
        el.style.inset = getComputedStyle(el).padding;
        el.style.position = 'absolute';
        el.style.padding = '0';
    }

    function ensureButtonStructure(button) {
        var rec = BUTTON_STRUCT.get(button);
        if (rec) return rec;

        var image = document.createElement('button-image');
        image.className = 'button_image';
        // No `.button_image` rule in any of the three stylesheets declares its own
        // background-repeat or background-size (only `.close_image_button > .button_image`
        // sets contain/center explicitly) -- so a browser's own initial value for
        // background-repeat, "repeat", was what row 8 (applySpriteSizes) saw, and treated
        // this as a TILING sprite: it wrote the atlas's raw pixel size (e.g. 128px) straight
        // onto a ~68px box instead of scaling it down to fit. `WidgetImage`'s own fit here is
        // ImageFit/contain, the same "fill the box, keep the aspect" `.square_button` itself
        // already uses for its own sprite -- setting it explicitly, the same three
        // declarations row 5's tick already carries, makes row 8 correctly take its CONTAIN
        // branch (which only steps in when the sprite is smaller than the box) instead of
        // its REPEAT branch, and gives a correct scale even before row 8's message arrives.
        image.style.backgroundPosition = 'center';
        image.style.backgroundRepeat = 'no-repeat';
        image.style.backgroundSize = 'contain';
        var label = document.createElement('button-label');
        label.className = 'button_label';

        // The engine's WidgetLabel draws the button's text, never the button itself.
        label.textContent = button.textContent;
        button.textContent = '';

        button.appendChild(image);
        button.appendChild(label);
        positionButtonChild(image);
        positionButtonChild(label);

        // UpdateLayout also sets m_label.TextAlign, not just its box: VerticalCenter|
        // HorizontalCenter by default, Left|Top when --button-layout is "textleft"
        // (WidgetButtonLayout.TextLeft), and left untouched for "custom" (Layout & Custom
        // -- CSS's own text-align is authoritative there, e.g. item_button's). A browser's
        // default text flow is already left/top, which is why the padding numbers alone
        // (verified against WidgetButton.cs -- Size - textPadding.Size, Position =
        // textPadding.TopLeft, the same formula the image uses, in the same CSS top/right/
        // bottom/left order getComputedStyle and `inset` both use) can measure correct while
        // the centred buttons still look wrong: nothing centred the text inside that box.
        var layout = getComputedStyle(button).getPropertyValue('--button-layout').trim();
        if (layout !== 'custom') {
            label.style.display = 'flex';
            label.style.alignItems = layout === 'textleft' ? 'flex-start' : 'center';
            label.style.justifyContent = layout === 'textleft' ? 'flex-start' : 'center';
        }

        rec = { image: image, label: label };
        BUTTON_STRUCT.set(button, rec);
        return rec;
    }

    // Hovering label[for] already puts its target in :hover for free (measured:
    // Chrome matches #target:hover with only the label under the pointer).
    function syncLabelColor(label) {
        var forId = label.getAttribute('for');
        var target = forId && document.getElementById(forId);
        if (!target) return;
        // Mirror the CONTROL, never the wrapper. `input[type="checkbox"]:hover` carries the
        // colour (defaults.css), and hovering a label[for] puts its control in :hover for free,
        // so the input is both the element the rule reaches and the element whose state is
        // already right. The wrapper carries no rule at all now that `.checkbox` is gone.
        //
        // Clearing rather than returning is the other half. label.style.color is an INLINE
        // style and outranks every stylesheet rule, so a one-way write sticks forever: once the
        // hovered #ffaa33 was copied on, nothing ever took it off and it beat .checkbox_label's
        // own #cceeff for the rest of the session. Dropping the inline value hands the colour
        // back to the cascade, which is where it belongs whenever there is nothing to mirror.
        if (target.tagName.toLowerCase() === 'input' && target.type === 'checkbox')
            label.style.color = getComputedStyle(target).color;
        else
            label.style.color = '';
    }

    function onStateEvent(e) {
        if (!(e.target instanceof Element)) return;
        sync(e.target);
        syncCheckboxTick(e.target);
        applySpriteSizeOnStateChange(e.target); // row 8: a state change can swap e.target's own sprite (:hover/:focus/:disabled); re-derive its size only if it actually changed

        // The tick actually receives :hover, so an event landing on it must be
        // traced back to the input for the tint and :checked opacity to stay live.
        var wrap = e.target.closest && e.target.closest('checkbox-frame'); // the tag this file creates, never a project class name
        var boxInput = wrap && wrap.querySelector('input[type="checkbox"]');
        if (boxInput) syncCheckboxTick(boxInput);

        // button-image/button-label now cover most of a button's own box (row 11), so a
        // real pointer landing on the button hits one of them, not the button -- pointerover
        // bubbles, but e.target stays the child, and sync()/row 8 above only ever acted on
        // e.target. Traced back the same way the checkbox tick is above: without this, a
        // button's own tint/frame/opacity never updates for :hover at all (measured: the
        // clone kept its resting 30%/no-tint values through a real mouse hover).
        var btn = e.target.closest && e.target.closest('button');
        if (btn && btn !== e.target) {
            sync(btn);
            applySpriteSizeOnStateChange(btn);
        }

        var labels = document.querySelectorAll('label[for]');
        for (var i = 0; i < labels.length; i++) syncLabelColor(labels[i]);
    }

    // Row 8 (runmobile_design.md row 8): a <view> fragment carries a ratio, never a
    // pixel size, so a repeating or contain-fitted background guesses wrong (see the
    // .back_pattern comment in ui.css). Each atlas the stylesheet references reports
    // its sprites' real sizes by postMessage from a hidden <object>; this applies
    // them only where a browser would otherwise guess. Degrades silently: Inkscape
    // and Illustrator are expected to strip the reporter on save, and then neither
    // case ever runs -- a repeating background stays scaled to the element and the
    // checkbox tick stays stretched, exactly today's behaviour.
    var SPRITE_SIZES = {}; // "file#id" -> [w, h, frameW, frameH, offsetX, offsetY], filled as reporters answer
                           // (an atlas packed before the reporter forwarded frames sends [w, h]; both are accepted)

    // A browser resolves url()'s path to an absolute one in computed style, but the
    // reporter names its file with document.URL.split("/").pop() -- basename only
    // -- so both sides have to compare on that same basename, not the full path.
    function parseSpriteRef(value) {
        var m = /url\(["']?([^"'()#]+\.svg)#([^"'()]+)["']?\)/i.exec(value || '');
        return m ? { file: m[1].split('/').pop(), id: m[2] } : null;
    }

    // Collects every atlas the stylesheet actually names, so this works for
    // several atlases and never hardcodes one filename.
    function findAtlasFiles() {
        var files = {};
        var all = document.querySelectorAll('*');
        for (var i = 0; i < all.length; i++) {
            var el = all[i];
            var computed = getComputedStyle(el);
            var bg = parseSpriteRef(computed.backgroundImage);
            if (bg) files[bg.file] = true;
            var border = parseSpriteRef(computed.borderImageSource);
            if (border) files[border.file] = true;
            if (el.tagName && el.tagName.toLowerCase() === 'img') {
                var m = /^([^#]+\.svg)#/i.exec(el.getAttribute('src') || '');
                if (m) files[m[1].split('/').pop()] = true;
            }
        }
        return Object.keys(files);
    }

    // `repeat` gets the sprite's own size so it tiles at native scale -- whatever size is
    // declared, never just `auto`: `.back_pattern` now declares `background-size: 100%`
    // (ui.css comment on that rule), which a browser would otherwise stretch to the panel
    // instead of tiling. `contain` (the checkbox tick, built above) gets it too so it draws
    // unstretched, gated on the computed value alone since only a keyword `contain` reaches here.
    // Unconditional -- always re-derives from the live cascade -- because a caller (sync(),
    // below) uses this to correct a size it just overwrote as a side effect; a caller that
    // wants to skip untouched elements gates it itself (applySpriteSizeOnStateChange).
    // The atlas reports [w, h, frameW, frameH, offsetX, offsetY]: `w/h` are the PACKED pixels
    // (the artwork with its transparent margin trimmed off) and the frame is the untrimmed cell
    // the artist drew in, with the artwork sitting inside it at the offset. 493 of 644 sprites in
    // star_ui.svg are trimmed, so this is the normal case, not an exotic one.
    //
    // The engine fits the FRAME to the widget box and lets the artwork ride inside it:
    // WidgetBackground divides by Sprite.Size, which is OriginalWidth/Height (data-frame), never
    // the packed size, and the GPU quad shifts by `pivot*OriginalWidth - OffsetX` then spans only
    // the packed extent (GLHelper.cs). A browser handed a <view> fragment sees the packed rect
    // ALONE -- no frame, no offset -- so `contain` spreads the artwork across the whole box:
    // check_icon is 20x20 inside a 54x54 frame, so it drew 2.7x too large and cornered instead of
    // inset. Reproducing the engine here is two declarations, a size and a position, both scaled
    // by the same factor the frame needs to reach the box.
    function applySpriteSizeTo(el) {
        el.style.backgroundSize = '';     // drop what a previous state set, so the reads below see
        el.style.backgroundPosition = ''; // the live cascade and not our own leftover override
        var computed = getComputedStyle(el);
        var ref = parseSpriteRef(computed.backgroundImage);
        var size = ref && SPRITE_SIZES[ref.file + '#' + ref.id];
        if (!size) return;

        var w = size[0], h = size[1];
        // Two values, not six, from an atlas packed before the reporter forwarded frames. Then the
        // sprite is treated as its own frame and every line below is a no-op -- old atlas, old
        // behaviour, no crash.
        var fw = size.length >= 6 ? size[2] : w, fh = size.length >= 6 ? size[3] : h;
        var ox = size.length >= 6 ? size[4] : 0, oy = size.length >= 6 ? size[5] : 0;

        // A tile has to be told how big one tile is, and an SVG fragment reference reports no
        // intrinsic size for the browser to work it out. BOTH conditions, not either: an icon
        // drawn once needs no tile size and gets stretched by one, and `repeat` is CSS's INITIAL
        // value, so an element that never mentioned background-repeat reads as tiling.
        // Frame-aware tiling is NOT expressible in CSS -- one background layer cannot tile a frame
        // with the artwork inset -- so a trimmed tile would still be wrong here. No such sprite
        // exists: back_pattern is the only repeating one in the stylesheets and it is untrimmed.
        if (computed.backgroundRepeat === 'repeat'
            && computed.backgroundSize !== 'contain' && computed.backgroundSize !== 'cover') {
            el.style.backgroundSize = w + 'px ' + h + 'px';
            return;
        }

        // Untrimmed: the frame IS the artwork, so the browser's own fit already matches the engine
        // and writing our own numbers over it would only add rounding. 151 sprites take this exit.
        if (fw === w && fh === h && ox === 0 && oy === 0) return;

        var W = el.clientWidth, H = el.clientHeight; // padding box -- what a background is drawn into
        if (!(W > 0 && H > 0)) return;

        // How the FRAME reaches the box, per keyword, mirroring what the widget does with the same
        // intent: `contain` is ImageFit (one scale, the smaller), `cover` its opposite, and a
        // percentage or `auto` is ImageStretch, whose FlatScale is a per-axis vector divide.
        var sx, sy, bs = computed.backgroundSize;
        if (bs === 'contain') sx = sy = Math.min(W / fw, H / fh);
        else if (bs === 'cover') sx = sy = Math.max(W / fw, H / fh);
        else if (bs === 'auto' || bs.indexOf('%') !== -1) { sx = W / fw; sy = H / fh; }
        else return; // an explicit pixel size is the author's own statement, not ours to reinterpret

        // Where the scaled frame's top-left lands, then the artwork's own offset within it. The
        // centring term is the engine's default BackgroundPivot of (0.5, 0.5); it falls out to zero
        // whenever the frame fills the box exactly, which is every stretch case.
        el.style.backgroundSize = (w * sx) + 'px ' + (h * sy) + 'px';
        el.style.backgroundPosition = ((W - fw * sx) / 2 + ox * sx) + 'px '
                                    + ((H - fh * sy) / 2 + oy * sy) + 'px';
    }

    function applySpriteSizes() {
        var all = document.querySelectorAll('*');
        for (var i = 0; i < all.length; i++) applySpriteSizeTo(all[i]);
    }

    // A `:hover`/`:focus`/`:disabled` rule can swap an element's OWN `background-image` to a
    // different sprite (`.circle_button:hover`, `.hhex_image_button:hover`) without going
    // through sync()'s tint/opacity clone at all, so row 8 needs its own hook on the same
    // per-target state-event path row 3/5 already use (onStateEvent) -- not a document-wide
    // rescan on every pointerover, which would be a full querySelectorAll('*') plus a
    // getComputedStyle per element on every mouse move over every widget.
    //
    // Gated on the sprite actually changing, and this is the part that matters: an element
    // with no state rule at all (back_pattern) still receives pointerover/pointerout as the
    // cursor crosses it, resolves to the SAME sprite every time, and must end up writing
    // NOTHING -- back_pattern has no interactive state, so nothing here has any business
    // touching it. (Measured root cause of "hover breaks the tiling pattern": that write was
    // never the bug by itself, sync()'s FRAME_PROPS copy overwriting the clone was -- fixed
    // there, in sync() -- but this element still had no reason to be written to either.)
    var SPRITE_SIZE_KEY = new WeakMap(); // el -> the sprite key ("file#id", or null) last seen for it here

    function applySpriteSizeOnStateChange(el) {
        var ref = parseSpriteRef(getComputedStyle(el).backgroundImage);
        var key = ref ? ref.file + '#' + ref.id : null;
        if (SPRITE_SIZE_KEY.has(el) && SPRITE_SIZE_KEY.get(el) === key) return; // same sprite as last time -- leave el untouched
        SPRITE_SIZE_KEY.set(el, key);
        applySpriteSizeTo(el);
    }

    function onSvgMessage(e) {
        var data = e.data;
        if (!data || data.type !== 'svg-views' || !data.views) return;
        Object.keys(data.views).forEach(function (id) {
            SPRITE_SIZES[data.file + '#' + id] = data.views[id];
        });
        console.log('runmobile_design row 8: sizes received from ' + data.file);
        applySpriteSizes();
    }

    // The reporter runs at SVG parse time, before the <object>'s own `load` fires,
    // so its postMessage is already queued by then; the extra tick just lets that
    // queued message be handled first before this decides nothing is coming.
    function loadAtlasReporters() {
        var files = findAtlasFiles();
        if (!files.length) return;
        window.addEventListener('message', onSvgMessage);
        files.forEach(function (file) {
            var obj = document.createElement('object');
            obj.type = 'image/svg+xml';
            obj.data = file;
            obj.style.cssText = 'position:absolute; width:0; height:0; border:none;';
            obj.addEventListener('load', function () {
                setTimeout(function () {
                    var prefix = file + '#';
                    var any = Object.keys(SPRITE_SIZES).some(function (key) { return key.indexOf(prefix) === 0; });
                    if (!any) console.log('runmobile_design row 8: no reporter in ' + file + ', sizes not applied');
                }, 200);
            });
            document.body.appendChild(obj);
        });
    }

    // Row 9 (runmobile_design.md row 9): the STATIC resets that used to be set here, one
    // inline style per property, now live in runmobile_design.css instead -- a stylesheet
    // sits in the normal cascade (an author rule of equal specificity overrides it without
    // `!important`) and applies before first paint (no flash of Chrome's own border/padding
    // while this script is still running). See that file for what is reset and why.
    //
    // That includes forcing `<dialog>` to render without `open`: a plain `dialog { display:
    // block; }` in that stylesheet beats the UA's `dialog:not([open]) { display: none }` by
    // cascade origin, not selector specificity, so no script-side attribute-forcing is
    // needed any more (measured in Chrome, see that file's own comment). There is nothing
    // left here for dialogs -- a stylesheet can set `display`, just never `open` itself.

    // Row 9 (runmobile_design.md row 9): `overflow: hidden` already clips the widget's
    // content in a browser, so the engine's few-pixel clip-path inset buys nothing there
    // and costs the whole border-image (measured: the frame draws without clip-path and
    // vanishes with it). The engine keeps its own inset; only the browser drops it.
    // Static, so one pass at init is enough; nothing here changes with state.
    var CLIP_PATH_INSET = /^inset\(/;

    // Chrome computes `overflow: hidden` down to the used value `clip` on a text-editing
    // form control (measured: an <input> reports "clip", a <textarea> still reports
    // "hidden") -- both already clip the element in a browser, so both count here.
    function dropRedundantClipPath(el) {
        var computed = getComputedStyle(el);
        var clips = computed.overflow === 'hidden' || computed.overflow === 'clip';
        if (clips && CLIP_PATH_INSET.test(computed.clipPath)) el.style.clipPath = 'none';
    }

    function init() {
        var all = document.body.querySelectorAll('*');
        for (var i = 0; i < all.length; i++) {
            sync(all[i]);
            syncCheckboxTick(all[i]);
            dropRedundantClipPath(all[i]);
        }
        var buttons = document.body.querySelectorAll('button');
        for (var b = 0; b < buttons.length; b++) ensureButtonStructure(buttons[b]);
        var labels = document.body.querySelectorAll('label[for]');
        for (var l = 0; l < labels.length; l++) syncLabelColor(labels[l]);

        // Event-driven, not a CSS `:has()` mirror: `:focus` also swaps WHICH SPRITE
        // shows (border-image-source), not just the tint, so the clone needs the
        // widget's live computed style to track both; focusin/out bubble, pointerover/out need capture.
        var events = ['pointerover', 'pointerout', 'focusin', 'focusout', 'change'];
        events.forEach(function (name) {
            document.body.addEventListener(name, onStateEvent, true);
        });

        loadAtlasReporters();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
