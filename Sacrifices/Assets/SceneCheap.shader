// Compiled shader for all platforms, uncompressed size: 10.3KB

Shader "Futile/SceneCheap" {
Properties {
 _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
}
SubShader { 
 Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }


 // Stats for Vertex shader:
 //       d3d11 : 5 math
 //        d3d9 : 6 math
 //        gles : 3 math, 1 texture
 //       gles3 : 3 math, 1 texture
 //   glesdesktop : 3 math, 1 texture
 //       metal : 3 math
 //      opengl : 6 math
 // Stats for Fragment shader:
 //       d3d11 : 2 math, 1 texture
 //        d3d9 : 4 math, 1 texture
 //       metal : 3 math, 1 texture
 //      opengl : 5 math, 1 texture
 Pass {
  Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" }
  BindChannels {
   Bind "vertex", Vertex
   Bind "color", Color
   Bind "texcoord", TexCoord
  }
  ZWrite Off
  Cull Off
  Fog {
   Color (0,0,0,0)
  }
  Blend SrcAlpha OneMinusSrcAlpha
Program "vp" {
SubProgram "opengl " {
// Stats: 6 math
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Vector 5 [_MainTex_ST]
"3.0-!!ARBvp1.0
PARAM c[6] = { program.local[0],
		state.matrix.mvp,
		program.local[5] };
MAD result.texcoord[0].xy, vertex.texcoord[0], c[5], c[5].zwzw;
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
MOV result.color.x, vertex.color.w;
END
# 6 instructions, 0 R-regs
"
}
SubProgram "d3d9 " {
// Stats: 6 math
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Matrix 0 [glstate_matrix_mvp]
Vector 4 [_MainTex_ST]
"vs_3_0
dcl_position o0
dcl_texcoord0 o1
dcl_color0 o2
dcl_position0 v0
dcl_texcoord0 v1
dcl_color0 v2
mad o1.xy, v1, c4, c4.zwzw
dp4 o0.w, v0, c3
dp4 o0.z, v0, c2
dp4 o0.y, v0, c1
dp4 o0.x, v0, c0
mov o2.x, v2.w
"
}
SubProgram "d3d11 " {
// Stats: 5 math
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
ConstBuffer "$Globals" 64
Vector 48 [_MainTex_ST]
ConstBuffer "UnityPerDraw" 336
Matrix 0 [glstate_matrix_mvp]
BindCB  "$Globals" 0
BindCB  "UnityPerDraw" 1
"vs_4_0
eefiecedlfkilkfppcmjofnnkaffepfhbknebohbabaaaaaamiacaaaaadaaaaaa
cmaaaaaapeaaaaaagiabaaaaejfdeheomaaaaaaaagaaaaaaaiaaaaaajiaaaaaa
aaaaaaaaaaaaaaaaadaaaaaaaaaaaaaaapapaaaakbaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaabaaaaaaapaaaaaakjaaaaaaaaaaaaaaaaaaaaaaadaaaaaaacaaaaaa
ahaaaaaalaaaaaaaaaaaaaaaaaaaaaaaadaaaaaaadaaaaaaapadaaaalaaaaaaa
abaaaaaaaaaaaaaaadaaaaaaaeaaaaaaapaaaaaaljaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaafaaaaaaapaiaaaafaepfdejfeejepeoaafeebeoehefeofeaaeoepfc
enebemaafeeffiedepepfceeaaedepemepfcaaklepfdeheogmaaaaaaadaaaaaa
aiaaaaaafaaaaaaaaaaaaaaaabaaaaaaadaaaaaaaaaaaaaaapaaaaaafmaaaaaa
aaaaaaaaaaaaaaaaadaaaaaaabaaaaaaadamaaaagfaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaabaaaaaaaealaaaafdfgfpfaepfdejfeejepeoaafeeffiedepepfcee
aaedepemepfcaaklfdeieefcfiabaaaaeaaaabaafgaaaaaafjaaaaaeegiocaaa
aaaaaaaaaeaaaaaafjaaaaaeegiocaaaabaaaaaaaeaaaaaafpaaaaadpcbabaaa
aaaaaaaafpaaaaaddcbabaaaadaaaaaafpaaaaadicbabaaaafaaaaaaghaaaaae
pccabaaaaaaaaaaaabaaaaaagfaaaaaddccabaaaabaaaaaagfaaaaadeccabaaa
abaaaaaagiaaaaacabaaaaaadiaaaaaipcaabaaaaaaaaaaafgbfbaaaaaaaaaaa
egiocaaaabaaaaaaabaaaaaadcaaaaakpcaabaaaaaaaaaaaegiocaaaabaaaaaa
aaaaaaaaagbabaaaaaaaaaaaegaobaaaaaaaaaaadcaaaaakpcaabaaaaaaaaaaa
egiocaaaabaaaaaaacaaaaaakgbkbaaaaaaaaaaaegaobaaaaaaaaaaadcaaaaak
pccabaaaaaaaaaaaegiocaaaabaaaaaaadaaaaaapgbpbaaaaaaaaaaaegaobaaa
aaaaaaaadcaaaaaldccabaaaabaaaaaaegbabaaaadaaaaaaegiacaaaaaaaaaaa
adaaaaaaogikcaaaaaaaaaaaadaaaaaadgaaaaafeccabaaaabaaaaaadkbabaaa
afaaaaaadoaaaaab"
}
SubProgram "gles " {
// Stats: 3 math, 1 textures
"!!GLES


#ifdef VERTEX

attribute vec4 _glesVertex;
attribute vec4 _glesColor;
attribute vec4 _glesMultiTexCoord0;
uniform highp mat4 glstate_matrix_mvp;
uniform highp vec4 _MainTex_ST;
varying highp vec2 xlv_TEXCOORD0;
varying highp float xlv_COLOR;
void main ()
{
  highp float tmpvar_1;
  lowp float tmpvar_2;
  tmpvar_2 = _glesColor.w;
  tmpvar_1 = tmpvar_2;
  gl_Position = (glstate_matrix_mvp * _glesVertex);
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
  xlv_COLOR = tmpvar_1;
}



#endif
#ifdef FRAGMENT

uniform sampler2D _MainTex;
varying highp vec2 xlv_TEXCOORD0;
varying highp float xlv_COLOR;
void main ()
{
  mediump vec4 col_1;
  highp vec2 tmpvar_2;
  tmpvar_2.x = xlv_TEXCOORD0.x;
  tmpvar_2.y = ((xlv_TEXCOORD0.y * 0.5) + 0.5);
  lowp vec4 tmpvar_3;
  tmpvar_3 = texture2D (_MainTex, tmpvar_2);
  col_1 = tmpvar_3;
  highp float tmpvar_4;
  tmpvar_4 = (col_1.w * xlv_COLOR);
  col_1.w = tmpvar_4;
  gl_FragData[0] = col_1;
}



#endif"
}
SubProgram "glesdesktop " {
// Stats: 3 math, 1 textures
"!!GLES


#ifdef VERTEX

attribute vec4 _glesVertex;
attribute vec4 _glesColor;
attribute vec4 _glesMultiTexCoord0;
uniform highp mat4 glstate_matrix_mvp;
uniform highp vec4 _MainTex_ST;
varying highp vec2 xlv_TEXCOORD0;
varying highp float xlv_COLOR;
void main ()
{
  highp float tmpvar_1;
  lowp float tmpvar_2;
  tmpvar_2 = _glesColor.w;
  tmpvar_1 = tmpvar_2;
  gl_Position = (glstate_matrix_mvp * _glesVertex);
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
  xlv_COLOR = tmpvar_1;
}



#endif
#ifdef FRAGMENT

uniform sampler2D _MainTex;
varying highp vec2 xlv_TEXCOORD0;
varying highp float xlv_COLOR;
void main ()
{
  mediump vec4 col_1;
  highp vec2 tmpvar_2;
  tmpvar_2.x = xlv_TEXCOORD0.x;
  tmpvar_2.y = ((xlv_TEXCOORD0.y * 0.5) + 0.5);
  lowp vec4 tmpvar_3;
  tmpvar_3 = texture2D (_MainTex, tmpvar_2);
  col_1 = tmpvar_3;
  highp float tmpvar_4;
  tmpvar_4 = (col_1.w * xlv_COLOR);
  col_1.w = tmpvar_4;
  gl_FragData[0] = col_1;
}



#endif"
}
SubProgram "gles3 " {
// Stats: 3 math, 1 textures
"!!GLES3#version 300 es


#ifdef VERTEX


in vec4 _glesVertex;
in vec4 _glesColor;
in vec4 _glesMultiTexCoord0;
uniform highp mat4 glstate_matrix_mvp;
uniform highp vec4 _MainTex_ST;
out highp vec2 xlv_TEXCOORD0;
out highp float xlv_COLOR;
void main ()
{
  highp float tmpvar_1;
  lowp float tmpvar_2;
  tmpvar_2 = _glesColor.w;
  tmpvar_1 = tmpvar_2;
  gl_Position = (glstate_matrix_mvp * _glesVertex);
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
  xlv_COLOR = tmpvar_1;
}



#endif
#ifdef FRAGMENT


layout(location=0) out mediump vec4 _glesFragData[4];
uniform sampler2D _MainTex;
in highp vec2 xlv_TEXCOORD0;
in highp float xlv_COLOR;
void main ()
{
  mediump vec4 col_1;
  highp vec2 tmpvar_2;
  tmpvar_2.x = xlv_TEXCOORD0.x;
  tmpvar_2.y = ((xlv_TEXCOORD0.y * 0.5) + 0.5);
  lowp vec4 tmpvar_3;
  tmpvar_3 = texture (_MainTex, tmpvar_2);
  col_1 = tmpvar_3;
  highp float tmpvar_4;
  tmpvar_4 = (col_1.w * xlv_COLOR);
  col_1.w = tmpvar_4;
  _glesFragData[0] = col_1;
}



#endif"
}
SubProgram "metal " {
// Stats: 3 math
Bind "vertex" ATTR0
Bind "color" ATTR1
Bind "texcoord" ATTR2
ConstBuffer "$Globals" 80
Matrix 0 [glstate_matrix_mvp]
Vector 64 [_MainTex_ST]
"metal_vs
#include <metal_stdlib>
using namespace metal;
struct xlatMtlShaderInput {
  float4 _glesVertex [[attribute(0)]];
  float4 _glesColor [[attribute(1)]];
  float4 _glesMultiTexCoord0 [[attribute(2)]];
};
struct xlatMtlShaderOutput {
  float4 gl_Position [[position]];
  float2 xlv_TEXCOORD0;
  float xlv_COLOR;
};
struct xlatMtlShaderUniform {
  float4x4 glstate_matrix_mvp;
  float4 _MainTex_ST;
};
vertex xlatMtlShaderOutput xlatMtlMain (xlatMtlShaderInput _mtl_i [[stage_in]], constant xlatMtlShaderUniform& _mtl_u [[buffer(0)]])
{
  xlatMtlShaderOutput _mtl_o;
  half4 tmpvar_1;
  tmpvar_1 = half4(_mtl_i._glesColor);
  float tmpvar_2;
  half tmpvar_3;
  tmpvar_3 = tmpvar_1.w;
  tmpvar_2 = float(tmpvar_3);
  _mtl_o.gl_Position = (_mtl_u.glstate_matrix_mvp * _mtl_i._glesVertex);
  _mtl_o.xlv_TEXCOORD0 = ((_mtl_i._glesMultiTexCoord0.xy * _mtl_u._MainTex_ST.xy) + _mtl_u._MainTex_ST.zw);
  _mtl_o.xlv_COLOR = tmpvar_2;
  return _mtl_o;
}

"
}
}
Program "fp" {
SubProgram "opengl " {
// Stats: 5 math, 1 textures
SetTexture 0 [_MainTex] 2D 0
"3.0-!!ARBfp1.0
PARAM c[1] = { { 0.5 } };
TEMP R0;
MAD R0.y, fragment.texcoord[0], c[0].x, c[0].x;
MOV R0.x, fragment.texcoord[0];
TEX R0, R0, texture[0], 2D;
MUL result.color.w, R0, fragment.color.primary.x;
MOV result.color.xyz, R0;
END
# 5 instructions, 1 R-regs
"
}
SubProgram "d3d9 " {
// Stats: 4 math, 1 textures
SetTexture 0 [_MainTex] 2D 0
"ps_3_0
dcl_2d s0
def c0, 0.50000000, 0, 0, 0
dcl_texcoord0 v0.xy
dcl_color0 v1.x
mad r0.y, v0, c0.x, c0.x
mov r0.x, v0
texld r0, r0, s0
mul_pp oC0.w, r0, v1.x
mov_pp oC0.xyz, r0
"
}
SubProgram "d3d11 " {
// Stats: 2 math, 1 textures
SetTexture 0 [_MainTex] 2D 0
"ps_4_0
eefiecedmkhgffabeifiklegkpomcognajnlogbhabaaaaaamaabaaaaadaaaaaa
cmaaaaaakaaaaaaaneaaaaaaejfdeheogmaaaaaaadaaaaaaaiaaaaaafaaaaaaa
aaaaaaaaabaaaaaaadaaaaaaaaaaaaaaapaaaaaafmaaaaaaaaaaaaaaaaaaaaaa
adaaaaaaabaaaaaaadadaaaagfaaaaaaaaaaaaaaaaaaaaaaadaaaaaaabaaaaaa
aeaeaaaafdfgfpfaepfdejfeejepeoaafeeffiedepepfceeaaedepemepfcaakl
epfdeheocmaaaaaaabaaaaaaaiaaaaaacaaaaaaaaaaaaaaaaaaaaaaaadaaaaaa
aaaaaaaaapaaaaaafdfgfpfegbhcghgfheaaklklfdeieefcoeaaaaaaeaaaaaaa
djaaaaaafkaaaaadaagabaaaaaaaaaaafibiaaaeaahabaaaaaaaaaaaffffaaaa
gcbaaaaddcbabaaaabaaaaaagcbaaaadecbabaaaabaaaaaagfaaaaadpccabaaa
aaaaaaaagiaaaaacabaaaaaadcaaaaapdcaabaaaaaaaaaaaegbabaaaabaaaaaa
aceaaaaaaaaaiadpaaaaaadpaaaaaaaaaaaaaaaaaceaaaaaaaaaaaaaaaaaaadp
aaaaaaaaaaaaaaaaefaaaaajpcaabaaaaaaaaaaaegaabaaaaaaaaaaaeghobaaa
aaaaaaaaaagabaaaaaaaaaaadiaaaaahiccabaaaaaaaaaaadkaabaaaaaaaaaaa
ckbabaaaabaaaaaadgaaaaafhccabaaaaaaaaaaaegacbaaaaaaaaaaadoaaaaab
"
}
SubProgram "gles " {
"!!GLES"
}
SubProgram "glesdesktop " {
"!!GLES"
}
SubProgram "gles3 " {
"!!GLES3"
}
SubProgram "metal " {
// Stats: 3 math, 1 textures
SetTexture 0 [_MainTex] 2D 0
"metal_fs
#include <metal_stdlib>
using namespace metal;
struct xlatMtlShaderInput {
  float2 xlv_TEXCOORD0;
  float xlv_COLOR;
};
struct xlatMtlShaderOutput {
  half4 _glesFragData_0 [[color(0)]];
};
struct xlatMtlShaderUniform {
};
fragment xlatMtlShaderOutput xlatMtlMain (xlatMtlShaderInput _mtl_i [[stage_in]], constant xlatMtlShaderUniform& _mtl_u [[buffer(0)]]
  ,   texture2d<half> _MainTex [[texture(0)]], sampler _mtlsmp__MainTex [[sampler(0)]])
{
  xlatMtlShaderOutput _mtl_o;
  half4 col_1;
  float2 tmpvar_2;
  tmpvar_2.x = _mtl_i.xlv_TEXCOORD0.x;
  tmpvar_2.y = ((_mtl_i.xlv_TEXCOORD0.y * 0.5) + 0.5);
  half4 tmpvar_3;
  tmpvar_3 = _MainTex.sample(_mtlsmp__MainTex, (float2)(tmpvar_2));
  col_1 = tmpvar_3;
  float tmpvar_4;
  tmpvar_4 = ((float)col_1.w * _mtl_i.xlv_COLOR);
  col_1.w = half(tmpvar_4);
  _mtl_o._glesFragData_0 = col_1;
  return _mtl_o;
}

"
}
}
 }
}
}