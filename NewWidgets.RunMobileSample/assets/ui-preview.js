/* ui-preview.js -- browser-preview tint.
 *
 * The engine draws a widget's frame as `texture(...) * Color`, a per-channel
 * multiply of the sprite by the widget's tint. An SVG feColorMatrix with a
 * diagonal reproduces that exactly:
 *
 *   <filter color-interpolation-filters="sRGB">
 *     <feColorMatrix type="matrix" values="R 0 0 0 0  0 G 0 0 0  0 0 B 0 0  0 0 0 1 0"/>
 *   </filter>
 *
 * with R,G,B = tint channel / 255 and the alpha row left identity (measured
 * max error < 1 level of 255; color-interpolation-filters="sRGB" is
 * mandatory or the default linearRGB space puts blue ~76 levels out).
 *
 * ui.css carries the tint in a custom property, `--background-color`, on
 * whatever element it applies to (plus `--background-opacity` for the
 * multiplicative alpha some rules also carry). Almost every occurrence of
 * both is on a state selector -- textedit:hover, button:disabled, :focus --
 * so the tint has to track live interaction state, not just the value at
 * page load.
 *
 * Construction: a static wrapper plus a filtered, childless clone, kept in
 * sync with the widget's OWN computed style on every state-changing event.
 *
 *   - `filter` applies to the element's WHOLE SUBTREE (measured: white text
 *     rendered 252,234,137 under a tint filter placed on its parent), so
 *     the filter can never sit on the widget itself when the widget has
 *     text or children.
 *   - `::before` generates no box on `<input>` (spec), and this dialog's
 *     text field, password field, checkbox and the "custom server" edit are
 *     all `<input>`, so a pseudo-element construction cannot tint them.
 *   - `filter` also makes its element a containing block for absolutely
 *     positioned descendants (measured: a child moved from left 0 to 44),
 *     so it must not land on a widget that has positioned children either.
 *
 * So: each tinted widget is wrapped in a plain `display:contents` <div>.
 * That wrapper generates no box of its own, so it is never a containing
 * block and never disturbs the widget's original absolute position or its
 * descendants' containing block -- the widget stays exactly where it was.
 * Inside the wrapper, a new empty <div> clone is inserted before the
 * widget, carrying copies of only the properties that draw its frame
 * (border-image-*, background-*, clip-path, box geometry) plus the tint
 * filter. The widget's own frame is then suppressed inline
 * (border-image-source / background-image: none) so only the clone draws
 * the tinted frame; the widget itself, unfiltered and painted on top,
 * draws only its text or native content.
 *
 * Why event-driven and not a CSS `:has()` mirror. `.clone:has(~ input:hover)`
 * (verified: Chrome resolves that selector) could drive the FILTER from the
 * widget's live state without any JS. It cannot drive the whole picture,
 * though: :focus on a text edit also swaps which sprite is showing
 * (`panel_white_hovered_9` unfocused, `panel_white_normal_9` focused) via a
 * plain border-image-source rule that has nothing to do with tint, and the
 * clone's frame properties are copies, not a live reference -- a
 * :has()-driven filter would sit on top of whichever sprite the clone
 * happened to be built with, not the one the widget is currently showing.
 * The clone's frame needs to track state exactly as much as its tint does,
 * and re-reading the widget's own computed style already does both
 * correctly in one place, because that computed style already reflects
 * every rule and every state ui.css defines -- nothing here re-derives or
 * duplicates that cascade.
 *
 * A shared <filter> cannot read var(--background-color) from the widget --
 * it resolves var() at its own position in the tree -- so one filter is
 * emitted per distinct colour actually found, keyed by its hex.
 *
 * `url("ui.svg#name")` addresses a sprite correctly on its own -- Chrome
 * resolves the fragment against ui.svg's own <view id="name"> elements, for
 * background-image, border-image-source and <img src> alike (measured; an
 * earlier version of this script assumed otherwise and carried a duplicate,
 * hand-maintained table of every sprite's atlas rectangle to work around it
 * -- wrong premise, and all of that is gone). The one place a fragment does
 * NOT resolve is `mask-image` (measured: a broken-image icon), which is why
 * the checkbox tick below is a tinted, filtered clone of the sprite -- the
 * same construction as a widget frame -- rather than a mask.
 *
 * Plain ES2020, no dependencies.
 */
(function () {
    'use strict';

    // Properties that draw a widget's own frame -- copied from the widget
    // onto its filtered clone so the clone renders the identical frame.
    var FRAME_PROPS = [
        'position', 'left', 'top', 'width', 'height', 'margin',
        'boxSizing', 'borderStyle', 'borderWidth', 'borderColor',
        'borderImageSource', 'borderImageSlice', 'borderImageWidth',
        'borderImageOutset', 'borderImageRepeat',
        'backgroundImage', 'backgroundSize', 'backgroundPosition',
        'backgroundRepeat', 'backgroundClip', 'clipPath'
    ];

    // ui.css's only forms for a tint colour: a 6-digit hex or rgba()/rgb().
    // `transparent` (never a real tint) resolves to null, same as any other
    // unparsed value.
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

    // Reads a custom property on `el`, but only if `el` itself declares it --
    // a custom property inherits by default, and ui.css sets
    // `--background-color` once on <html> purely for the page backdrop, so
    // every element without its own declaration would otherwise read that
    // inherited value back as if it were its own tint. Comparing to the
    // parent's computed value tells the two cases apart: an inherited value
    // is always identical to the parent's, an element's own declaration
    // only coincidentally could be. ponytail: that coincidence is not
    // handled -- accepted as a simplification, it does not occur in ui.css.
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

    // Builds (once per distinct colour) the <filter> that reproduces the
    // engine's texture*Color multiply, and returns its id.
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
        filter.setAttribute('color-interpolation-filters', 'sRGB');
        var matrix = document.createElementNS(NS, 'feColorMatrix');
        matrix.setAttribute('type', 'matrix');
        var r = rgb[0] / 255, g = rgb[1] / 255, b = rgb[2] / 255;
        matrix.setAttribute('values',
            r.toFixed(6) + ' 0 0 0 0  0 ' + g.toFixed(6) + ' 0 0 0  0 0 ' + b.toFixed(6) + ' 0 0  0 0 0 1 0');
        filter.appendChild(matrix);
        defsSvg.appendChild(filter);
        return id;
    }

    // el -> { wrapper, clone }, created lazily the first time el needs a tint.
    var CLONES = new WeakMap();

    function getOrCreateClone(el) {
        var rec = CLONES.get(el);
        if (rec) return rec;

        var wrapper = document.createElement('div');
        wrapper.style.display = 'contents'; // no box of its own: not a containing block, disturbs nothing
        el.parentNode.insertBefore(wrapper, el);

        var clone = document.createElement('div');
        clone.setAttribute('aria-hidden', 'true');
        clone.style.pointerEvents = 'none';
        clone.style.display = 'none'; // shown by sync() once it has a real tint

        wrapper.appendChild(clone);
        wrapper.appendChild(el); // moves el under the wrapper, does not clone it

        rec = { wrapper: wrapper, clone: clone };
        CLONES.set(el, rec);
        return rec;
    }

    // Re-reads el's current --background-color / --background-opacity and
    // its current frame (border-image-*, background-*, geometry, clip-path)
    // and brings its clone up to date -- or, if el has no tint right now,
    // hides the clone and hands el's own frame back to it. Called once for
    // every element at load, and again on every event that could have
    // changed a :hover / :focus / :checked / :disabled match.
    function sync(el) {
        if (!el || el.id === 'nw-tint-defs') return;
        if (el === document.body || el === document.documentElement) return;

        var parent = el.parentElement;
        var colorText = ownValue(el, parent, '--background-color');
        var rgb = colorText ? parseColor(colorText) : null;
        var opacityText = ownValue(el, parent, '--background-opacity');
        var opacity = opacityText ? parseOpacity(opacityText) : null;

        var rec = CLONES.get(el);

        if (!rgb) {
            // No tint right now: if el was ever tinted before, give its own
            // frame back (drop the inline override, let ui.css's cascade --
            // hover/focus included -- draw it again) and hide the clone.
            if (rec) {
                rec.clone.style.display = 'none';
                el.style.borderImageSource = '';
                el.style.backgroundImage = '';
                el.style.backgroundColor = '';
            }
            // --background-opacity can stand alone (.back_pattern has no tint colour
            // of its own), and el itself is safe to dim directly here -- it is only
            // ever the plain frame a --background-color clone would otherwise need
            // to isolate from text, and an element with no tint has no clone.
            el.style.opacity = opacity !== null ? String(opacity) : '';
            return;
        }

        rec = getOrCreateClone(el);

        // el's own frame was suppressed by a previous sync() (if any), and
        // that inline override would otherwise poison the very read below --
        // getComputedStyle always sees the highest-specificity value, which
        // is now the inline one, not whatever :focus/:hover/:checked
        // currently asks ui.css for. Lift it before reading, so the frame
        // copied onto the clone is the one el would be showing right now.
        el.style.borderImageSource = '';
        el.style.backgroundImage = '';
        var computed = getComputedStyle(el);
        FRAME_PROPS.forEach(function (prop) { rec.clone.style[prop] = computed[prop]; });
        rec.clone.style.filter = 'url(#' + ensureFilter(rgb) + ')';
        rec.clone.style.opacity = opacity !== null ? String(opacity) : '';
        rec.clone.style.display = '';

        // Only the clone should draw the frame now. `background-color` is
        // real CSS, not the custom `--background-color` this script reads --
        // it is included here because `<input>`'s UA stylesheet paints it
        // opaque white regardless of `appearance:none` (measured: a hovered
        // text edit rendered flat white, the clone's tint fully hidden
        // behind it), and that would occlude the clone the same way an
        // unsuppressed border-image would.
        el.style.borderImageSource = 'none';
        el.style.backgroundImage = 'none';
        el.style.backgroundColor = 'transparent';
    }

    // The checkbox tick, `.checkbox #checkbox_image`. A browser gives an
    // <input> no children at all, so that selector never matched anything --
    // the fix is not to read the rule's values in script, but to build the
    // child the selector already expects, so CSS matches it and resolves
    // :hover itself. The wrapper takes over the input's own geometry and its
    // `.checkbox` class (so ui.css's background-image/:hover rules keep
    // drawing the frame, natively, with no script help); the tick covers the
    // same box as the input (a browser's :hover matches what the pointer is
    // actually over, so the two must occupy the same area to agree) and
    // sits on top of it, so it is what genuinely receives :hover, and
    // forwards clicks to the input it now covers.
    var CHECKBOX_STRUCT = new WeakMap();

    function ensureCheckboxStructure(input) {
        var rec = CHECKBOX_STRUCT.get(input);
        if (rec) return rec;

        var computed = getComputedStyle(input);
        var wrapper = document.createElement('div');
        wrapper.className = 'checkbox'; // matches `.checkbox`'s background-image etc from here on
        ['position', 'left', 'top', 'width', 'height', 'margin'].forEach(function (prop) {
            wrapper.style[prop] = computed[prop];
        });

        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);
        input.classList.remove('checkbox'); // the wrapper carries the frame now, not the control
        input.style.position = 'absolute';
        input.style.inset = '0';
        input.style.width = '100%';
        input.style.height = '100%';
        input.style.margin = '0';
        input.style.opacity = '0'; // invisible; the wrapper draws the sprite, the tick draws the mark

        var tick = document.createElement('div');
        tick.id = 'checkbox_image'; // a real id, so `.checkbox #checkbox_image` matches natively
        tick.setAttribute('aria-hidden', 'true');
        tick.style.position = 'absolute';
        tick.style.inset = '0';
        tick.style.width = '100%'; // `inset:0` alone is not enough: a bare <div> also matches
        tick.style.height = '100%'; // ui.css's `panel, div { width:100px; height:100px }`, an
        // explicit (non-auto) width, and CSS's own over-constraint rule (2.1 10.3.7) then
        // prefers that width over what `inset:0`'s implied left+right would give -- measured:
        // the tick rendered 100x100 over a 40x40 checkbox until these were set explicitly.
        tick.style.backgroundImage = 'url("ui.svg#check_icon")';
        tick.style.backgroundRepeat = 'no-repeat';
        tick.style.backgroundPosition = 'center';
        tick.style.backgroundSize = 'contain';
        tick.addEventListener('click', function () { input.click(); }); // topmost; forward the click
        wrapper.appendChild(tick);

        rec = { wrapper: wrapper, tick: tick };
        CHECKBOX_STRUCT.set(input, rec);
        return rec;
    }

    // Tints the tick's own check_icon sprite the same way a widget frame is
    // tinted (check_icon is a plain white glyph, so the multiply reproduces
    // a flat recolour exactly) and shows it only when checked --
    // WidgetCheckBox.DrawContents draws the tick conditionally in code, so
    // no CSS property expresses that in either vocabulary and this stays
    // script-side. The colour itself needs no branching for :hover: it is
    // read straight off the tick's own computed --image-color, which
    // already reflects `.checkbox #checkbox_image:hover` natively, because
    // the tick is a real element that is genuinely, natively :hover.
    function syncCheckboxTick(el) {
        // Not `el.matches('.checkbox')`: ensureCheckboxStructure moves that class onto the
        // new wrapper the first time this runs, so the input itself would fail that check
        // on every call after the first. `type` never changes.
        if (!(el.tagName && el.tagName.toLowerCase() === 'input' && el.type === 'checkbox')) return;
        var rec = ensureCheckboxStructure(el);
        var tick = rec.tick;
        var rgb = parseColor(getComputedStyle(tick).getPropertyValue('--image-color'));
        tick.style.filter = rgb ? 'url(#' + ensureFilter(rgb) + ')' : '';
        tick.style.opacity = el.matches(':checked') ? '1' : '0';
    }

    // Button padding, generated from `button .button_label`,
    // `.text_button .button_label` and `.image_button .button_image`.
    // WidgetButton.UpdateLayout reads padding off those two children, so a
    // browser's <button> -- content, no children -- needs it on itself
    // instead. None of the three is state-dependent, so this runs once. A
    // detached probe, carrying el's own classes verbatim so the real
    // cascade (base rule vs. a more specific one) resolves the same way it
    // would for el itself, stands in for the child ui.css's selector names.
    function applyButtonPadding(el) {
        if (el.tagName.toLowerCase() !== 'button') return; // XHTML keeps tagName's authored case, unlike HTML's forced upper
        var childClass = el.matches('.image_button') ? 'button_image' : 'button_label';
        var container = document.createElement('div');
        container.style.cssText = 'position:fixed; left:-99999px; top:-99999px;';
        container.innerHTML = '<button class="' + (el.className || '') + '"><span class="' + childClass + '"></span></button>';
        document.body.appendChild(container);
        el.style.padding = getComputedStyle(container.querySelector('.' + childClass)).padding;
        document.body.removeChild(container);
    }

    // Label <-> checkbox hover, general via for=, no id anywhere. Hovering
    // label[for] already puts its target in :hover for nothing (measured:
    // Chrome matches #target:hover with only the label under the pointer),
    // so only the reverse needs wiring: WidgetCheckBox.LinkedLabel is the
    // engine's own reason a checkbox's label lights with it, and that
    // binding is specific to a checkbox -- an ordinary text field's for=
    // label gets no such behaviour, in the engine or here.
    function syncLabelColor(label) {
        var forId = label.getAttribute('for');
        var target = forId && document.getElementById(forId);
        if (!target) return;
        if (target.tagName.toLowerCase() === 'input' && target.type === 'checkbox') {
            var rec = CHECKBOX_STRUCT.get(target);
            if (rec) target = rec.wrapper; // `.checkbox` moved to the wrapper; it carries `:hover`'s colour now
        }
        if (!target.matches('.checkbox')) return;
        label.style.color = getComputedStyle(target).color;
    }

    function onStateEvent(e) {
        if (!(e.target instanceof Element)) return;
        sync(e.target);
        syncCheckboxTick(e.target);

        // The tick is a new element around the real <input> and is what
        // actually receives :hover, so an event landing on it has to be
        // traced back to the input for the tint and the :checked opacity
        // to stay live.
        var wrap = e.target.closest && e.target.closest('.checkbox');
        var boxInput = wrap && wrap.querySelector('input[type="checkbox"]');
        if (boxInput) syncCheckboxTick(boxInput);

        var labels = document.querySelectorAll('label[for]');
        for (var i = 0; i < labels.length; i++) syncLabelColor(labels[i]);
    }

    // Row 1 + 2: root font-size and page background this engine has no <html>/<body>
    // to state them on -- one engine font unit is 30 browser pixels (the number
    // NewWidgets.BrowserPreview writes on its own reference render), and the black
    // backdrop and zero margin are this page's alone, not a widget's.
    function applyPageDefaults() {
        document.documentElement.style.fontSize = '30px';
        document.documentElement.style.margin = '0';
        document.documentElement.style.backgroundColor = '#000000';
        document.body.style.margin = '0';
        document.body.style.backgroundColor = '#000000';
    }

    function init() {
        applyPageDefaults();
        var all = document.body.querySelectorAll('*');
        for (var i = 0; i < all.length; i++) {
            sync(all[i]);
            syncCheckboxTick(all[i]);
        }
        var buttons = document.body.querySelectorAll('button');
        for (var b = 0; b < buttons.length; b++) applyButtonPadding(buttons[b]);
        var labels = document.body.querySelectorAll('label[for]');
        for (var l = 0; l < labels.length; l++) syncLabelColor(labels[l]);

        // :hover, :focus, :checked and :disabled are the pseudo-classes
        // ui.css keys a tint on. focusin/focusout bubble (focus/blur do
        // not), so capture is not required for them, but pointerover/out
        // don't reach a plain listener on an ancestor without it disabled,
        // and capture costs nothing extra here.
        var events = ['pointerover', 'pointerout', 'focusin', 'focusout', 'change'];
        events.forEach(function (name) {
            document.body.addEventListener(name, onStateEvent, true);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
