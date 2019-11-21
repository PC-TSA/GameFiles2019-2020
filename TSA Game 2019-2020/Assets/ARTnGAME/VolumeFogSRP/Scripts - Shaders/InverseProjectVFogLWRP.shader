Shader "Hidden/InverseProjectVFogLWRP"
{
    HLSLINCLUDE

    //#include "PostProcessing/Shaders/StdLib.hlsl"
	#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Colors.hlsl"

	#include "ClassicNoise3D.hlsl"
	//#include "SimplexNoise3D.hlsl"

    ///#define EXCLUDE_FAR_PLANE

    float4x4 unity_CameraInvProjection;

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
	TEXTURE2D_SAMPLER2D(_NoiseTex, sampler_NoiseTex);

	//MINE
#pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP2
#pragma multi_compile _ RADIAL_DIST
#pragma multi_compile _ USE_SKYBOX
	float _DistanceOffset;
	float _Density;
	float _LinearGrad;
	float _LinearOffs;
	float _Height;
	float _cameraRoll;
	//WORLD RECONSTRUCT	
	//float4x4 _InverseView;
	//float4x4 _camProjection;	////TO REMOVE
	// Fog/skybox information
	half4 _FogColor;
	samplerCUBE _SkyCubemap;
	half4 _SkyCubemap_HDR;
	half4 _SkyTint;
	half _SkyExposure;
	float _SkyRotation;
	float4 _cameraDiff;
	float _cameraTiltSign;

	float _NoiseDensity;
	float _NoiseScale;
	float3 _NoiseSpeed;
	float _NoiseThickness;
	float _OcclusionDrop;
	float _OcclusionExp;
	int noise3D = 0;

	// Applies one of standard fog formulas, given fog coordinate (i.e. distance)
	half ComputeFogFactor(float coord)
	{
		float fog = 0.0;
#if FOG_LINEAR
		// factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
		fog = coord * _LinearGrad + _LinearOffs;
#elif FOG_EXP
		// factor = exp(-density*z)
		fog = _Density * coord;
		fog = exp2(-fog);
#else // FOG_EXP2
		// factor = exp(-(density*z)^2)
		fog = _Density * coord;
		fog = exp2(-fog * fog);
#endif
		return saturate(fog);
	}
	// Distance-based fog
	float ComputeDistance(float3 ray, float depth)
	{
		float dist;
#if RADIAL_DIST
		dist = length(ray * depth);
#else
		dist = depth * _ProjectionParams.z;
#endif
		// Built-in fog starts at near plane, so match that by
		// subtracting the near value. Not a perfect approximation
		// if near plane is very large, but good enough.
		dist -= _ProjectionParams.y;
		return dist;
	}

	////LOCAL LIGHT
	float4 localLightColor;
	float4 localLightPos;

	/////////////////// SCATTER
	bool doDistance;
	bool doHeight;
	// Distance-based fog
	
	uniform float4 _CameraWS;
	//SM v1.7
	uniform float luminance, Multiplier1, Multiplier2, Multiplier3, bias, lumFac, contrast, turbidity;
	//uniform float mieDirectionalG = 0.7,0.913; 
	float mieDirectionalG;
	float mieCoefficient;//0.054
	float reileigh;


	//SM v1.7
	uniform sampler2D _ColorRamp;
	uniform float _Close;
	uniform float _Far;
	uniform float3 v3LightDir;		// light source
	uniform float FogSky;
	float4 _TintColor; //float3(680E-8, 1550E-8, 3450E-8);
	uniform float ClearSkyFac;	
	uniform float4 _HeightParams;

	// x = start distance
	uniform float4 _DistanceParams;

	int4 _SceneFogMode; // x = fog mode, y = use radial flag
	float4 _SceneFogParams;
#ifndef UNITY_APPLY_FOG
	half4 unity_FogColor;
	half4 unity_FogDensity;
#endif	


	uniform float e = 2.71828182845904523536028747135266249775724709369995957;
	uniform float pi = 3.141592653589793238462643383279502884197169;
	uniform float n = 1.0003;
	uniform float N = 2.545E25;
	uniform float pn = 0.035;
	uniform float3 lambda = float3(680E-9, 550E-9, 450E-9);
	uniform float3 K = float3(0.686, 0.678, 0.666);//const vec3 K = vec3(0.686, 0.678, 0.666);
	uniform float v = 4.0;
	uniform float rayleighZenithLength = 8.4E3;
	uniform float mieZenithLength = 1.25E3;
	uniform float EE = 1000.0;
	uniform float sunAngularDiameterCos = 0.999956676946448443553574619906976478926848692873900859324;
	// 66 arc seconds -> degrees, and the cosine of that
	float cutoffAngle = 3.141592653589793238462643383279502884197169 / 1.95;
	float steepness = 1.5;
	// Linear half-space fog, from https://www.terathon.com/lengyel/Lengyel-UnifiedFog.pdf
	float ComputeHalfSpace(float3 wsDir)
	{
		//float4 _HeightParams = float4(1,1,1,1);

		//wsDir.y = wsDir.y - abs(11.2*_cameraDiff.x);// -0.4;// +abs(11.2*_cameraDiff.x);

		float3 wpos = _CameraWS.xyz + wsDir; // _CameraWS + wsDir;
		
		float FH = _HeightParams.x;
		float3 C = _CameraWS.xyz;
		float3 V = wsDir;
		float3 P = wpos;
		float3 aV = _HeightParams.w * V;
		float FdotC = _HeightParams.y;
		float k = _HeightParams.z;
		float FdotP = P.y - FH;
		float FdotV = wsDir.y;
		float c1 = k * (FdotP + FdotC);
		float c2 = (1 - 2 * k) * FdotP;
		float g = min(c2, 0.0);
		g = -length(aV) * (c1 - g * g / abs(FdotV + 1.0e-5f));
		return g;
	}

	//SM v1.7
	float3 totalRayleigh(float3 lambda) {
		float pi = 3.141592653589793238462643383279502884197169;
		float n = 1.0003; // refraction of air
		float N = 2.545E25; //molecules per air unit volume 								
		float pn = 0.035;
		return (8.0 * pow(pi, 3.0) * pow(pow(n, 2.0) - 1.0, 2.0) * (6.0 + 3.0 * pn)) / (3.0 * N * pow(lambda, float3(4.0, 4.0, 4.0)) * (6.0 - 7.0 * pn));
	}

	float rayleighPhase(float cosTheta)
	{
		return (3.0 / 4.0) * (1.0 + pow(cosTheta, 2.0));
	}

	float3 totalMie(float3 lambda, float3 K, float T)
	{
		float pi = 3.141592653589793238462643383279502884197169;
		float v = 4.0;
		float c = (0.2 * T) * 10E-18;
		return 0.434 * c * pi * pow((2.0 * pi) / lambda, float3(v - 2.0, v - 2.0, v - 2.0)) * K;
	}

	float hgPhase(float cosTheta, float g)
	{
		float pi = 3.141592653589793238462643383279502884197169;
		return (1.0 / (4.0*pi)) * ((1.0 - pow(g, 2.0)) / pow(abs(1.0 - 2.0*g*cosTheta + pow(g, 2.0)), 1.5));
	}

	float sunIntensity(float zenithAngleCos)
	{
		float cutoffAngle = 3.141592653589793238462643383279502884197169 / 1.95;//pi/
		float steepness = 1.5;
		float EE = 1000.0;
		return EE * max(0.0, 1.0 - exp(-((cutoffAngle - acos(zenithAngleCos)) / steepness)));
	}

	float logLuminance(float3 c)
	{
		return log(c.r * 0.2126 + c.g * 0.7152 + c.b * 0.0722);
	}

	float3 tonemap(float3 HDR)
	{
		float Y = logLuminance(HDR);
		float low = exp(((Y*lumFac + (1.0 - lumFac))*luminance) - bias - contrast / 2.0);
		float high = exp(((Y*lumFac + (1.0 - lumFac))*luminance) - bias + contrast / 2.0);
		float3 ldr = (HDR.rgb - low) / (high - low);
		return float3(ldr);
	}

	/////////////////// END SCATTER


    half _Opacity;

    struct Varyings
    {
        float4 position : SV_Position;
        float2 texcoord : TEXCOORD0;
        float3 ray : TEXCOORD1;
		float2 uvFOG : TEXCOORD2;
		float4 interpolatedRay : TEXCOORD3;
    };

    // Vertex shader that procedurally outputs a full screen triangle
    Varyings Vertex(uint vertexID : SV_VertexID)
    {
        // Render settings
        float far = _ProjectionParams.z;
        float2 orthoSize = unity_OrthoParams.xy;
        float isOrtho = unity_OrthoParams.w; // 0: perspective, 1: orthographic

        // Vertex ID -> clip space vertex position
        float x = (vertexID != 1) ? -1 : 3;
        float y = (vertexID == 2) ? -3 : 1;
        float3 vpos = float3(x, y, 1.0);

        // Perspective: view space vertex position of the far plane
        float3 rayPers = mul(unity_CameraInvProjection, vpos.xyzz * far).xyz;
		//rayPers.y = rayPers.y - abs(_cameraDiff.x * 15111);

        // Orthographic: view space vertex position
        float3 rayOrtho = float3(orthoSize * vpos.xy, 0);

        Varyings o;
        o.position = float4(vpos.x, -vpos.y, 1, 1);
        o.texcoord = (vpos.xy + 1) / 2;
        o.ray = lerp(rayPers, rayOrtho, isOrtho);

		//MINE
		float3 vA = vpos;
		float deg = _cameraRoll;
		float alpha = deg * 3.14 / 180.0;
		float sina, cosa;
		sincos(alpha, sina, cosa);
		float2x2 m = float2x2(cosa, -sina, sina, cosa);
		
		float3 tmpV = float3(mul(m, vA.xy), vA.z).xyz;
		float2 uvFOG = TransformTriangleVertexToUV(tmpV.xy);
		o.uvFOG = uvFOG.xy;

		half index = vpos.z;
		o.interpolatedRay.xyz = vpos;  // _FrustumCornersWS[(int)index];
		o.interpolatedRay.w = index;

        return o;
    }

    float3 ComputeViewSpacePosition(Varyings input, float z)
    {
        // Render settings
        float near = _ProjectionParams.y;
        float far = _ProjectionParams.z;
        float isOrtho = unity_OrthoParams.w; // 0: perspective, 1: orthographic

        // Z buffer sample        

        // Far plane exclusion
        #if !defined(EXCLUDE_FAR_PLANE)
        float mask = 1;
        #elif defined(UNITY_REVERSED_Z)
        float mask = z > 0;
        #else
        float mask = z < 1;
        #endif

        // Perspective: view space position = ray * depth
        float3 vposPers = input.ray * Linear01Depth(z);

        // Orthographic: linear depth (with reverse-Z support)
        #if defined(UNITY_REVERSED_Z)
        float depthOrtho = -lerp(far, near, z);
        #else
        float depthOrtho = -lerp(near, far, z);
        #endif

        // Orthographic: view space position
        float3 vposOrtho = float3(input.ray.xy, depthOrtho);

        // Result: view space position
        return lerp(vposPers, vposOrtho, isOrtho) * mask;
    }

    half4 VisualizePosition(Varyings input, float3 pos)
    {
        const float grid = 5;
        const float width = 3;

        pos *= grid;

        // Detect borders with using derivatives.
        float3 fw = fwidth(pos);
        half3 bc = saturate(width - abs(1 - 2 * frac(pos)) / fw);

        // Frequency filter
        half3 f1 = smoothstep(1 / grid, 2 / grid, fw);
        half3 f2 = smoothstep(2 / grid, 4 / grid, fw);
        bc = lerp(lerp(bc, 0.5, f1), 0, f2);

        // Blend with the source color.
        half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
        c.rgb = SRGBToLinear(lerp(LinearToSRGB(c.rgb), bc, 0.5));

        return c;
    }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

			float4x4 _InverseView;
	
	float2 WorldToScreenPos(float3 pos) {
		pos = normalize(pos - _WorldSpaceCameraPos)*(_ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y)) + _WorldSpaceCameraPos;
		float2 uv = float2(0,0);
		float4 toCam = mul(unity_WorldToCamera, float4(pos.xyz,1));
		float camPosZ = toCam.z;
		float height = 2 * camPosZ / unity_CameraProjection._m11;
		float width = _ScreenParams.x / _ScreenParams.y * height;
		uv.x = (toCam.x + width / 2) / width;
		uv.y = (toCam.y + height / 2) / height;
		return uv;
	}

	float2 raySphereIntersect(float3 r0, float3 rd, float3 s0, float sr) {
		
		float a = dot(rd, rd);
		float3 s0_r0 = r0 - s0;
		float b = 2.0 * dot(rd, s0_r0);
		float c = dot(s0_r0, s0_r0) - (sr * sr);
		float disc = b * b - 4.0 * a* c;
		if (disc < 0.0) {
			return float2(-1.0, -1.0);
		}
		else {
			return float2(-b - sqrt(disc), -b + sqrt(disc)) / (2.0 * a);
		}
	}

	float3x3 rotationMatrix(float3 axis, float angle)
	{
		axis = normalize(axis);
		float s = sin(angle);
		float c = cos(angle);
		float oc = 1.0 - c;
		
		return float3x3 (oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s,
			oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s,
			oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c);
	}
	float4x4 rotationMatrix4(float3 axis, float angle)
	{
		axis = normalize(axis);
		float s = sin(angle);
		float c = cos(angle);
		float oc = 1.0 - c;

		return float4x4 (oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s, 0.0,
		oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s, 0.0,
		oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c, 0.0,
		0.0, 0.0, 0.0, 1.0);
		
	}

            half4 Fragment(Varyings input) : SV_Target
            {
				float3 forward = mul((float3x3)(unity_WorldToCamera), float3(0, 0, 1));					
				float zsample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord);
				float3 vpos = ComputeViewSpacePosition(input, zsample);
				float3 wpos = mul(_InverseView, float4(vpos, 1)).xyz;				
				float depth = Linear01Depth(zsample * (zsample < 1.0));
				float4 wsDir = depth * float4(input.ray, 1); // input.interpolatedRay;				
				float4 wsPos = _CameraWS + wsDir;

				///// SCATTER
				float3 lightDirection = float3(-v3LightDir.x - 0*_cameraDiff.w * forward.x, -v3LightDir.y - 0 * _cameraDiff.w * forward.y, v3LightDir.z);
				
				//int noise3D = 0;
				half4 noise;
				half4 noise1;
				half4 noise2;
				if (noise3D == 0) {
					float fixFactor1 = 0;
					float fixFactor2 = 0;
					float dividerScale = 1; //1
					float scaler1 = 1.00; //0.05
					float scaler2 = 0.8; //0.01
					float scaler3 = 0.3; //0.01
					float signer1 = 0.004 / (dividerScale * 1.0);//0.4 !!!! (0.005 for 1) (0.4 for 0.05) //0.004
					float signer2 = 0.004 / (dividerScale * 1.0);//0.001
																
					if (_cameraDiff.w < 0) {
						fixFactor1 = -signer1 * 90 * 2 * 2210 / 1 * (dividerScale / 1);//2210
						fixFactor2 = -signer2 * 90 * 2 * 2210 / 1 * (dividerScale / 1);
					}
					float hor1 = -_cameraDiff.w * signer1 *_cameraDiff.y * 2210 / 1 * (dividerScale / 1) - 1.2 * _WorldSpaceCameraPos.x * 10 + fixFactor1;
					float hor2 = -_cameraDiff.w * signer2 *_cameraDiff.y * 2210 / 1 * (dividerScale / 1) - 1.2 * _WorldSpaceCameraPos.x * 10 + fixFactor2;
					float hor3 = -_cameraDiff.w * signer2 *_cameraDiff.y * 1210 / 1 * (dividerScale / 1) - 1.2 * _WorldSpaceCameraPos.x * 2 + fixFactor2;

					float vert1 = _cameraTiltSign * _cameraDiff.x * 0.77 * 1.05 * 160 + 0.0157*_cameraDiff.y * (pow((input.texcoord.x - 0.1), 2)) - 0.3 * _WorldSpaceCameraPos.y * 30
						- 2 * 0.33 * _WorldSpaceCameraPos.z * 2.13 + 50 * abs(cos(_WorldSpaceCameraPos.z * 0.01)) + 35 * abs(sin(_WorldSpaceCameraPos.z * 0.005));

						float vert2 = _cameraTiltSign * _cameraDiff.x * 0.20 * 1.05 * 160 + 0.0157*_cameraDiff.y * (pow((input.texcoord.x - 0.1), 2)) - 0.3 * _WorldSpaceCameraPos.y * 30
						- 1 * 0.33 * _WorldSpaceCameraPos.z * 3.24 + 75 * abs(sin(_WorldSpaceCameraPos.z * 0.02)) + 85 * abs(cos(_WorldSpaceCameraPos.z * 0.01));

						float vert3 = _cameraTiltSign * _cameraDiff.x * 0.10 * 1.05 * 70 + 0.0117*_cameraDiff.y * (pow((input.texcoord.x - 0.1), 2)) - 0.3 * _WorldSpaceCameraPos.y * 30
						- 1 * 1.03 * _WorldSpaceCameraPos.z * 3.24 + 75 * abs(sin(_WorldSpaceCameraPos.z * 0.02)) + 85 * abs(cos(_WorldSpaceCameraPos.z * 0.01));

					 noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, 1 * (dividerScale * (float2(input.texcoord.x*scaler1 * 1, input.texcoord.y*scaler1))
						+ (-0.001*float2((0.94)*hor1, vert1)) + 3 * abs(cos(_Time.y *1.22* 0.012)))) * 2 * 9;
					 noise1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, 1 * (dividerScale * (input.texcoord.xy*scaler2)
						+ (-0.001*float2((0.94)*hor2, vert2) + 3 * abs(cos(_Time.y *1.22* 0.010))))) * 3 * 9;
					 noise2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, 1 * (dividerScale * (input.texcoord.xy*scaler3)
						+ (-0.001*float2((0.94)*hor3, vert3) + 1 * abs(cos(_Time.y *1.22* 0.006))))) * 3 * 9;
				}
				else {

					/////////// NOISE 3D //////////////
					const float epsilon = 0.0001;

					float2 uv = input.texcoord * 4.0 + float2(0.2, 1) * _Time.y * 0.01;

					/*#if defined(SNOISE_AGRAD) || defined(SNOISE_NGRAD)
						#if defined(THREED)
											float3 o = 0.5;
						#else
											float2 o = 0.5;
						#endif
					#else*/
										float o = 0.5 * 1.5;
					//#endif

										float s = 0.011;

					/*#if defined(SNOISE)
										float w = 0.25;
					#else*/
										float w = 0.02;
					//#endif

					//#ifdef FRACTAL
										for (int i = 0; i < 5; i++)
					//#endif
					{							
											float3 coord = wpos + float3(_Time.y * 3 * _NoiseSpeed.x,
												_Time.y * _NoiseSpeed.y,
												_Time.y * _NoiseSpeed.z);
											float3 period = float3(s, s, 1.0) * 1111;

						

					//#if defined(CNOISE)
											o += cnoise(coord * 0.17 * _NoiseScale) * w;

											float3 pointToCamera = (wpos - _WorldSpaceCameraPos) * 0.47;
											int steps = 2;
											float stepCount = 1;
											float step = length(pointToCamera)/steps;
											for (int j = 0; j < steps; j++) {
												//ray trace noise												
												float3 coordAlongRay = _WorldSpaceCameraPos + normalize(pointToCamera) * step
													+ float3(_Time.y * 6 * _NoiseSpeed.x,
														_Time.y * _NoiseSpeed.y,
														_Time.y * _NoiseSpeed.z);
												o += 1.5*cnoise(coordAlongRay * 0.17 * _NoiseScale) * w * 1;
												//stepCount++;
												if (depth < 0.99999) {
													o += depth * 45 * _NoiseThickness;
												}
												step = step + step;
											}
				
						s *= 2.0;
						w *= 0.5;
					}
					noise = float4(o, o, o, 1); 
					noise1 = float4(o, o, o, 1); 
					noise2 = float4(o, o, o, 1);					
				}
				
				float cosTheta = dot(normalize(wsDir.xyz), lightDirection);
				cosTheta = dot(normalize(wsDir.xyz), -lightDirection);
				
				float lumChange = clamp(luminance * pow(abs(((1 - depth) / (_OcclusionDrop * 0.1 * 2))), _OcclusionExp), luminance, luminance * 2);
				if (depth <= _OcclusionDrop * 0.1 * 1) {						
					luminance = lerp(4*luminance, 1 * luminance, (0.001 * 1) / (_OcclusionDrop * 0.1 - depth + 0.001));
				}					

				float3 up = float3(0.0, 1.0, 0.0); //float3(0.0, 0.0, 1.0);			
				float3 lambda = float3(680E-8, 550E-8, 450E-8);
				float3 K = float3(0.686, 0.678, 0.666);
				float  rayleighZenithLength = 8.4E3;
				float  mieZenithLength = 1.25E3;				
				float  pi = 3.141592653589793238462643383279502884197169;
				float3 betaR = totalRayleigh(lambda) * reileigh * 1000;
				float3 lambda1 = float3(_TintColor.r, _TintColor.g, _TintColor.b)*0.0000001;//  680E-8, 1550E-8, 3450E-8);
				lambda = lambda1;
				float3 betaM = totalMie(lambda1, K, turbidity * Multiplier2) * mieCoefficient;
				float zenithAngle = acos(max(0.0, dot(up, normalize(lightDirection))));
				float sR = rayleighZenithLength / (cos(zenithAngle) + 0.15 * pow(abs(93.885 - ((zenithAngle * 180.0) / pi)), -1.253));
				float sM = mieZenithLength / (cos(zenithAngle) + 0.15 * pow(abs(93.885 - ((zenithAngle * 180.0) / pi)), -1.253));
				float  rPhase = rayleighPhase(cosTheta*0.5 + 0.5);
				float3 betaRTheta = betaR * rPhase ;
				float  mPhase = hgPhase(cosTheta, mieDirectionalG ) * Multiplier1;
				float3 betaMTheta = betaM * mPhase;
				float3 Fex = exp(-(betaR * sR + betaM * sM));
				float  sunE = sunIntensity(dot(lightDirection, up));
				float3 Lin = ((betaRTheta + betaMTheta) / (betaR + betaM)) * (1 - Fex) + sunE * Multiplier3*0.0001;
				float  sunsize = 0.0001;
				float3 L0 = 1.5 * Fex + (sunE * 1.0 * Fex)*sunsize;
				float3 FragColor = tonemap(Lin + L0);
				///// END SCATTER

				//occlusion !!!!
				half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord.xy);
				
				float3 subtractor = saturate(pow(abs(dot(normalize(input.ray), normalize(lightDirection))),36)) - (float3(1, 1, 1)*depth * 1);
				if (depth < _OcclusionDrop * 0.1) {
					FragColor = saturate(FragColor * pow(abs((depth / (_OcclusionDrop * 0.1))), _OcclusionExp));
				}
				else {
					if(depth < 0.9999) {
						FragColor = saturate(FragColor * pow(abs((depth / (_OcclusionDrop * 0.1))), 0.001));
					}
				}				

				//SCATTER
				int doHeightA = 1;
				int doDistanceA = 1;
				float g = ComputeDistance(input.ray, depth) - _DistanceOffset; 
				if (doDistanceA==1) {
					g += ComputeDistance(input.ray, depth) - _DistanceOffset;
				}
				if (doHeightA == 1) {
					g += ComputeHalfSpace(wpos);
				}
				
				g = g * pow(abs((noise.r + 1 * noise1.r + _NoiseDensity * noise2.r* 1)), 1.2)*0.3;
				
				half fogFac = ComputeFogFactor(max(0.0, g));
				if (zsample >= 0.999995) {
					if (FogSky <= 0) {
						fogFac = 1.0;
					}
					else {
						if (doDistance) {
							fogFac = fogFac * ClearSkyFac;
						}
					}
				}				
				
				float4 Final_fog_color = lerp(unity_FogColor + float4(FragColor, 1), sceneColor, fogFac);
				float fogHeight = _Height;
				half fog = ComputeFogFactor(max(0.0, g));

				//local light
				float3 visual = 0;// VisualizePosition(input, wpos);
				if (1 == 1) {
					
					float3 light1 = localLightPos.xyz;
					float dist1 = length(light1 - wpos);

					float2 screenPos = WorldToScreenPos(light1);
					float lightRadius = localLightColor.w;
					
					float dist2 = length(screenPos - float2(input.texcoord.x, input.texcoord.y * 0.62 + 0.23));					
					if (
						length(_WorldSpaceCameraPos - wpos) < length(_WorldSpaceCameraPos - light1) - lightRadius
						&&
						dot(normalize(_WorldSpaceCameraPos - wpos), normalize(_WorldSpaceCameraPos - light1)) > 0.95// 0.999
						) { //occlusion
					}
					else {						
						float factorOcclusionDist = length(_WorldSpaceCameraPos - wpos) - (length(_WorldSpaceCameraPos - light1) - lightRadius);
						float factorOcclusionDot = dot(normalize(_WorldSpaceCameraPos - wpos), normalize(_WorldSpaceCameraPos - light1));
					
						Final_fog_color = lerp(Final_fog_color,
							Final_fog_color  * (1 - ((11 - dist2) / 11)) 
							+ Final_fog_color * float4(2 * localLightColor.x, 2 * localLightColor.y, 2 * localLightColor.z, 1)*(11 - dist2) / 11,							
							(localLightPos.w * saturate(1 * 0.1458 / pow(dist2, 0.95)) 
								+ 0.04*saturate(pow(1 - input.uvFOG.y * (1 - fogHeight), 1.0)) - 0.04)
						);						
					}									
				}

#if USE_SKYBOX
				// Look up the skybox color.
				half3 skyColor = DecodeHDR(texCUBE(_SkyCubemap, i.ray), _SkyCubemap_HDR);
				skyColor *= _SkyTint.rgb * _SkyExposure * unity_ColorSpaceDouble;
				// Lerp between source color to skybox color with fog amount.
				return lerp(half4(skyColor, 1), sceneColor, fog);
#else
				// Lerp between source color to fog color with the fog amount.
				half4 skyColor = lerp(_FogColor, sceneColor, saturate(fog));

				float distToWhite = (Final_fog_color.r - 0.99) + (Final_fog_color.g - 0.99) + (Final_fog_color.b - 0.99);
				
				Final_fog_color = Final_fog_color + 0.0*Final_fog_color * float4(8,2,0,1);

				return Final_fog_color * _FogColor + float4(visual, 0);
#endif					                
            }

            ENDHLSL
        }
        Pass
        {
            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment Fragment

            float4x4 _InverseView;

            half4 Fragment(Varyings input) : SV_Target
            {
				float z = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord);
                float3 vpos = ComputeViewSpacePosition(input,z);
                float3 wpos = mul(_InverseView, float4(vpos, 1)).xyz;
                return VisualizePosition(input, wpos);
            }

            ENDHLSL
        }
    }
}