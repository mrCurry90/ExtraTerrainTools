using System;
using System.IO;
using Timberborn.AssetSystem;
using Timberborn.Beavers;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.UIElements;
using EaserFunction = TerrainTools.Easer.Function;

namespace TerrainTools.NoiseGenerator
{
    public class NoiseGeneratorToolPanel : ITerrainToolFragment
    {    
        private readonly NoiseGeneratorTool _tool;
        private readonly TerrainToolPanelFactory _elementFactory;
        private readonly BeaverNameService _nameService;
        
        private VisualElement _noiseGeneratorPanel;

        private Button _generateButton;
        private Label _hintLabel;
        private Label _loadingLabel;
        private Toggle _modeToggle;

        private TextField _seedField;
        private Toggle _lockSeedToggle;
        private Slider _octavesSlider = null;
        private Label _octavesSliderValue = null;
        private Slider _ampSlider = null;
        private Label _ampSliderValue = null;
        private Slider _freqSlider = null;
        private Label _freqSliderValue = null;
        private Slider _perXSlider = null;
        private Label _perXSliderValue = null;
        private Slider _perYSlider = null;
        private Label _perYSliderValue = null;
        private Slider _floorSlider = null;
        private Label _floorSliderValue = null;
        private Slider _midSlider = null;
        private Label _midSliderValue = null;
        private Slider _ceilingSlider = null;
        private Label _ceilingSliderValue = null;
        private Slider _baseSelectSlider = null;
        private Image _baseSelectSliderImage = null;
        private Slider _crestSelectSlider = null;
        private Image _crestSelectSliderImage = null;

        private readonly Texture2D[] curveTexturesIn;
        private readonly Texture2D[] curveTexturesOut;
        private readonly EaserFunction[] curveFunctions;

        private readonly static string texturePath = "Sprites/NoiseGeneratorToolPanel/38x30";

        private readonly static string idleText = "--- Idle ---";
        private readonly static string loadingText = "--- Generating: {0}% ---";
        private readonly static string hintText = "<i>Click <b>Generate</b> to update the terrain.</i>";
        private readonly static string updateWarning = "<i><color=#FFA500>All water sources</color> will be removed. This action cannot be undone. </i>";
        private readonly static string clearWarning = "<i><color=#FFA500>All objects</color> will be removed. This action cannot be undone.</i>";
        
        public NoiseGeneratorToolPanel( TerrainToolPanelFactory toolPanelFactory, EventBus eventBus, NoiseGeneratorTool noiseGeneratorTool, BeaverNameService nameService, IAssetLoader assetLoader ) 
        : base(eventBus, typeof(NoiseGeneratorTool))
        {
            _elementFactory = toolPanelFactory;
            _tool = noiseGeneratorTool;
            _nameService = nameService;
            ToolOrder = 100;

            curveFunctions = new EaserFunction[] {
                EaserFunction.Line,
                EaserFunction.Sine,
                EaserFunction.Quad,
                EaserFunction.Cube,
                EaserFunction.Quart,
                EaserFunction.Quint,
                EaserFunction.Expo,
                EaserFunction.Circ,
                EaserFunction.Back,
                EaserFunction.Elastic,
                EaserFunction.Bounce
            };

            try
            {
                var lineTex = assetLoader.Load<Texture2D>(
                        TexPath("Line")
                    );


                curveTexturesIn = new Texture2D[] {
                    lineTex,
                    assetLoader.Load<Texture2D>( TexPath("InSine") ),
                    assetLoader.Load<Texture2D>( TexPath("InQuad") ),
                    assetLoader.Load<Texture2D>( TexPath("InCubic") ),
                    assetLoader.Load<Texture2D>( TexPath("InQuart") ),
                    assetLoader.Load<Texture2D>( TexPath("InQuint") ),
                    assetLoader.Load<Texture2D>( TexPath("InExpo") ),
                    assetLoader.Load<Texture2D>( TexPath("InCirc") ),
                    assetLoader.Load<Texture2D>( TexPath("InBack") ),
                    assetLoader.Load<Texture2D>( TexPath("InElastic") ),
                    assetLoader.Load<Texture2D>( TexPath("InBounce") )
                };
                curveTexturesOut = new Texture2D[] {
                    lineTex,
                    assetLoader.Load<Texture2D>( TexPath("OutSine") ),
                    assetLoader.Load<Texture2D>( TexPath("OutQuad") ),
                    assetLoader.Load<Texture2D>( TexPath("OutCubic") ),
                    assetLoader.Load<Texture2D>( TexPath("OutQuart") ),
                    assetLoader.Load<Texture2D>( TexPath("OutQuint") ),
                    assetLoader.Load<Texture2D>( TexPath("OutExpo") ),
                    assetLoader.Load<Texture2D>( TexPath("OutCirc") ),
                    assetLoader.Load<Texture2D>( TexPath("OutBack") ),
                    assetLoader.Load<Texture2D>( TexPath("OutElastic") ),
                    assetLoader.Load<Texture2D>( TexPath("OutBounce") )
                };
            }
            catch(Exception err) {
                throw new NoiseGeneratorToolPanelException( "Failed to load textures for NoiseGeneratorToolPanel", err);
            }
        }
        public override VisualElement BuildToolPanelContent()
        {
            _noiseGeneratorPanel = _elementFactory.MakeTemplatePanel( FlexDirection.Column, Align.Stretch );
            var header  = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            _loadingLabel = _elementFactory.MakeLabel(idleText);
            var options = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            var footer = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );

            _loadingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _loadingLabel.style.marginTop = 5;
            _loadingLabel.style.marginBottom = 5;

            _noiseGeneratorPanel.Add(header);
            _noiseGeneratorPanel.Add(_loadingLabel);
            _noiseGeneratorPanel.Add(options);
            _noiseGeneratorPanel.Add(footer);      

#region NoiseGenerator.ToolContent.Header
            //--- Header ---//
            // Generate row
            var headerRow = _elementFactory.MakeContainer( FlexDirection.Row, Align.Center, Justify.SpaceBetween );
            header.Add( headerRow );            

            var headerRowLeft   = _elementFactory.MakeContainer( FlexDirection.Column, Align.FlexStart, Justify.Center );
            var headerRowRight  = _elementFactory.MakeContainer( FlexDirection.Column, Align.FlexEnd, Justify.Center );            
            headerRow.Add(headerRowLeft);
            headerRow.Add(headerRowRight);

            _generateButton = _elementFactory.MakeButton("Generate", delegate
            {
                OnClickGenerate();
            });
            headerRowLeft.Add( _generateButton );
            // Update Mode toggle
            _modeToggle = _elementFactory.MakeToggle( "Clear existing objects", delegate {
                UpdateHintText();
            });
            _modeToggle.style.alignSelf = Align.FlexEnd;
            headerRowRight.Add(_modeToggle);
    
            // Warning label row
            _hintLabel = _elementFactory.MakeLabel("WarningLabel");
            _hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add( _hintLabel );

            UpdateHintText();
#endregion NoiseGenerator.ToolContent.Header

#region NoiseGenerator.ToolContent.Options
            //-- Options --//
            // Seed row
            var seedRow = _elementFactory.MakeContainer( FlexDirection.Row, Align.Center, Justify.FlexStart );
            seedRow.style.paddingLeft = seedRow.style.paddingRight = 5;
            options.Add(seedRow);
            seedRow.Add(
                _elementFactory.MakeLabel("Seed: ")
            );

            _seedField = _elementFactory.MakeTextField();
            _seedField.style.flexGrow = 1;
            _seedField.style.fontSize = 12;
            _seedField.value = "-";
            seedRow.Add( _seedField );

            _lockSeedToggle = _elementFactory.MakeToggle("Lock", delegate {
                UpdateSeedFieldEnabled();
            });
            _lockSeedToggle.style.marginLeft = 3;
            seedRow.Add( _lockSeedToggle );

            // Level of detail
           options.Add( 
                MakeParameterSlider(
                    ref _octavesSlider, ref _octavesSliderValue, 
                    "Level of detail:", 
                    delegate {
                        // UpdateSliderValue(_octavesSlider, _octavesSliderValue, true); 
                        _octavesSlider.UpdateAsInt(_octavesSliderValue);
                    }, 
                    NoiseGenerator.Limits.Octaves.Min, 
                    NoiseGenerator.Limits.Octaves.Max,
                    NoiseGenerator.DefaultParameters.Octaves
                )             
            );            
            // Noise amplitude
            options.Add( 
                MakeParameterSlider(
                    ref _ampSlider, ref _ampSliderValue, 
                    "Amplitude:", 
                    delegate { 
                        // UpdateSliderValue(_ampSlider, _ampSliderValue, false);
                        _ampSlider.Update(_ampSliderValue);
                    }, 
                    NoiseGenerator.Limits.Amplitude.Min, 
                    NoiseGenerator.Limits.Amplitude.Max,
                    NoiseGenerator.DefaultParameters.Amplitude
                )             
            );            
            // Noise frequency
            options.Add( 
                MakeParameterSlider(
                    ref _freqSlider, ref _freqSliderValue, 
                    "Frequency:", 
                    delegate { 
                        // UpdateSliderValue(_freqSlider, _freqSliderValue, false); 
                        _freqSlider.Update(_freqSliderValue);
                    }, 
                    NoiseGenerator.Limits.Frequency.Min, 
                    NoiseGenerator.Limits.Frequency.Max,
                    NoiseGenerator.DefaultParameters.Frequency
                )             
            );
            // Crest curve
            options.Add( 
                MakeCurveSelector(
                    ref _crestSelectSlider, ref _crestSelectSliderImage, 
                    "Crest shape:", 
                    delegate { 
                        UpdateImageSelectorValue(_crestSelectSlider, _crestSelectSliderImage, curveTexturesOut); 
                    },
                    curveTexturesOut
                )             
            );

            // Base curve
            options.Add( 
                MakeCurveSelector(
                    ref _baseSelectSlider, ref _baseSelectSliderImage, 
                    "Base shape:", 
                    delegate { 
                        UpdateImageSelectorValue(_baseSelectSlider, _baseSelectSliderImage, curveTexturesIn); 
                    },
                    curveTexturesIn
                )             
            );
            // Mid
            options.Add( 
                MakeParameterSlider(
                    ref _midSlider, ref _midSliderValue, 
                    "Mean height:", 
                    delegate {
                        // UpdateSliderValue(_midSlider, _midSliderValue, true); 
                        _midSlider.UpdateAsInt(_midSliderValue);
                    }, 
                    NoiseGenerator.Limits.Mid.Min, 
                    NoiseGenerator.Limits.Mid.Max,
                    NoiseGenerator.DefaultParameters.Mid
                )             
            );
            // Floor
            options.Add( 
                MakeParameterSlider(
                    ref _floorSlider, ref _floorSliderValue, 
                    "Min height:", 
                    delegate { 
                        // UpdateSliderValue(_floorSlider, _floorSliderValue, true); 
                        _floorSlider.UpdateAsInt(_floorSliderValue);
                    }, 
                    NoiseGenerator.Limits.Floor.Min, 
                    NoiseGenerator.Limits.Floor.Max,
                    NoiseGenerator.DefaultParameters.Floor
                )             
            );
            
            // Ceiling
            options.Add( 
                MakeParameterSlider(
                    ref _ceilingSlider, ref _ceilingSliderValue, 
                    "Max height:", 
                    delegate { 
                        // UpdateSliderValue(_ceilingSlider, _ceilingSliderValue, true); 
                        _ceilingSlider.UpdateAsInt(_ceilingSliderValue);
                    }, 
                    NoiseGenerator.Limits.Ceiling.Min, 
                    NoiseGenerator.Limits.Ceiling.Max,
                    NoiseGenerator.DefaultParameters.Ceiling
                )             
            );
            // Noise period X 
            options.Add(
                MakeParameterSlider(
                    ref _perXSlider, ref _perXSliderValue,
                    "Period X:",
                    delegate {
                        // UpdateSliderValue(_perXSlider, _perXSliderValue, false);
                        _perXSlider.Update(_perXSliderValue);
                    },
                    NoiseGenerator.Limits.PeriodX.Min,
                    NoiseGenerator.Limits.PeriodX.Max,
                    NoiseGenerator.DefaultParameters.Period.x
                )             
            );
            // Noise period Y
            options.Add( 
                MakeParameterSlider(
                    ref _perYSlider, ref _perYSliderValue, 
                    "Period Y:", 
                    delegate { 
                        // UpdateSliderValue(_perYSlider, _perYSliderValue, false); 
                        _perYSlider.Update(_perYSliderValue);
                    }, 
                    NoiseGenerator.Limits.PeriodY.Min, 
                    NoiseGenerator.Limits.PeriodY.Max,
                    NoiseGenerator.DefaultParameters.Period.y
                )             
            );

#endregion NoiseGenerator.ToolContent.Options

#region NoiseGenerator.ToolContent.Footer
            var footerRow = _elementFactory.MakeContainer( FlexDirection.Row, Align.Center, Justify.FlexStart );
            footer.Add(footerRow);
            footerRow.Add(
                _elementFactory.MakeButton("Reset options", delegate
                {
                    SetDefaultOptions();
                })
            );
#endregion NoiseGenerator.ToolContent.Footer

            UpdateSeedFieldEnabled();
            SetDefaultOptions();

            return _noiseGeneratorPanel;
        }

        //-----------------------------------------------------
        // Header methods
        //-----------------------------------------------------
        private void UpdateHintText()
        {            
            _hintLabel.text = hintText + "\n" + (_modeToggle.value ? clearWarning : updateWarning);
        }

        private void OnClickGenerate() {
            if( _generateButton.enabledSelf )
            {
                if (!_lockSeedToggle.value)
                {
                    RandomizeSeed();
                }

                _tool.GenerateHeightMap( GetParameters(), _modeToggle.value );
            }
        }

        [OnEvent]
        public void OnGeneratorStarted(GeneratorStartedEvent evt)
        {
            _generateButton.SetEnabled(false);
            _loadingLabel.text = string.Format(loadingText, 0);
        }
        [OnEvent]
        public void OnGeneratorProgress(GeneratorProgressEvent evt)
        {
            _loadingLabel.text = string.Format(loadingText, Mathf.Round(100*evt.Progress));
        }
        [OnEvent]
        public void OnGeneratorFinished(GeneratorFinishedEvent evt)
        {
            _generateButton.SetEnabled(true);
            _loadingLabel.text = idleText;
        }
        //-----------------------------------------------------
        // Options methods
        //-----------------------------------------------------
        private void UpdateSeedFieldEnabled()
        {
            _seedField.SetEnabled(_lockSeedToggle.value);
        }
        private void RandomizeSeed()
        {
            _seedField.value = _nameService.RandomName();
        }
        // private void UpdateSliderValue(Slider slider, Label valueLabel, bool roundToInt = false) {            
        //     if(roundToInt && slider.value % 1 > 0) 
        //         slider.value = Mathf.Round(slider.value);
        //     valueLabel.text = slider.value.ToString();
        // }        
        private void UpdateImageSelectorValue(Slider slider, Image image, Texture2D[] array) {            
            if(slider.value % 1 > 0) 
                slider.value = Mathf.Round(slider.value);

            image.image = array[(int)slider.value];
        }
        private void UpdateSliders()
        {
            _octavesSlider.UpdateAsInt(_octavesSliderValue);
            _ampSlider.Update(_ampSliderValue);
            _freqSlider.Update(_freqSliderValue);
            _perXSlider.Update(_perXSliderValue);
            _perYSlider.Update(_perYSliderValue);
            _floorSlider.UpdateAsInt(_floorSliderValue);
            _midSlider.UpdateAsInt(_midSliderValue);
            _ceilingSlider.UpdateAsInt(_ceilingSliderValue);
            
            // UpdateSliderValue(_octavesSlider, _octavesSliderValue, true);
            // UpdateSliderValue(_ampSlider, _ampSliderValue, false);
            // UpdateSliderValue(_freqSlider, _freqSliderValue, false);
            // UpdateSliderValue(_perXSlider, _perXSliderValue, false);
            // UpdateSliderValue(_perYSlider, _perYSliderValue, false);
            // UpdateSliderValue(_floorSlider, _floorSliderValue, true);
            // UpdateSliderValue(_midSlider, _midSliderValue, true);
            // UpdateSliderValue(_ceilingSlider, _ceilingSliderValue, true);
        }

        private void UpdateImageSelectors()
        {
            UpdateImageSelectorValue(_baseSelectSlider, _baseSelectSliderImage, curveTexturesIn);
            UpdateImageSelectorValue(_crestSelectSlider, _crestSelectSliderImage, curveTexturesOut);
        }

        private NoiseParameters GetParameters()
        {
            var p = new NoiseParameters(
                _seedField.value == "" ? null : _seedField.value,
                (int)_octavesSlider.value,
                _ampSlider.value,
                _freqSlider.value,
                _perXSlider.value,
                _perYSlider.value,
                (int)_floorSlider.value,
                (int)_midSlider.value,
                (int)_ceilingSlider.value,
                curveFunctions[(int)_baseSelectSlider.value],
                curveFunctions[(int)_crestSelectSlider.value]
            );
       
            return p;
        }

        private VisualElement MakeParameterSlider(ref Slider sliderRef, ref Label labelRef, string label, Action action, float min, float max, float initial)
        {
            VisualElement _sliderContainer = _elementFactory.MakeSlider( label, action, min, max, initial );
            sliderRef = _sliderContainer.Q("Slider") as Slider;
            labelRef = _sliderContainer.Q("SliderValue") as Label;

            return _sliderContainer;
        }

        private VisualElement MakeCurveSelector( ref Slider sliderRef, ref Image imageRef, string label, Action action, Texture2D[] textureArray)
        {
            VisualElement _sliderContainer = _elementFactory.MakeSlider( label, action, 0, textureArray.Length - 1, 0 );
            sliderRef = _sliderContainer.Q("Slider") as Slider;
            var labelElem = _sliderContainer.Q("SliderValue") as Label;
            var parent = _sliderContainer.Q("SliderValue").hierarchy.parent;
            var index = parent.IndexOf(labelElem);

            imageRef = new Image();

            parent.RemoveAt(index);
            parent.Insert(index, imageRef);

            return _sliderContainer;
        }
        //-----------------------------------------------------
        // Footer methods
        //-----------------------------------------------------
        private void SetDefaultOptions()
        {
            var def = NoiseGenerator.DefaultParameters;

            _octavesSlider.value    = def.Octaves;
            _ampSlider.value        = def.Amplitude;
            _freqSlider.value       = def.Frequency;
            _perXSlider.value       = def.Period.x;
            _perYSlider.value       = def.Period.y;
            _floorSlider.value      = def.Floor;
            _midSlider.value        = def.Mid;
            _ceilingSlider.value    = def.Ceiling;

            _baseSelectSlider.value  = IndexOfFunction(NoiseGenerator.DefaultParameters.Base);
            _crestSelectSlider.value = IndexOfFunction(NoiseGenerator.DefaultParameters.Crest);

            UpdateSliders();
            UpdateImageSelectors();
        }

        //-----------------------------------------------------
        // Helper methods and exceptions
        //-----------------------------------------------------
        /// <summary>
        /// Spell funcName with capital first letter
        /// </summary>
        /// <param name="funcName"></param>
        /// <param name="dirIn"></param>
        /// <returns></returns>
        private string TexPath(string path)
        {
            return Path.Combine(texturePath, path);
        }

        private int IndexOfFunction(EaserFunction fnc)
        {
            for (int i = 0; i < curveFunctions.Length; i++)
            {
                if (curveFunctions[i] == fnc)
                    return i;
            }
            return -1;
        }

        [Serializable]
        public class NoiseGeneratorToolPanelException : Exception
        {
            public NoiseGeneratorToolPanelException() { }
            public NoiseGeneratorToolPanelException(string message) : base(message) { }
            public NoiseGeneratorToolPanelException(string message, System.Exception inner) : base(message, inner) { }
            protected NoiseGeneratorToolPanelException(
                System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }
}