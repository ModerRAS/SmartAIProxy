// Package security 提供 API Key/JWT 鉴权接口，便于网关中间件集成
package security

import (
	"errors"
	"strings"

	"github.com/golang-jwt/jwt/v4"
)

// APIKeyAuth 校验 API Key
func APIKeyAuth(key string, validKeys []string) bool {
	for _, k := range validKeys {
		if k == key {
			return true
		}
	}
	return false
}

// JWTAuth 校验 JWT Token
func JWTAuth(tokenStr string, secret string) (map[string]interface{}, error) {
	token, err := jwt.Parse(tokenStr, func(token *jwt.Token) (interface{}, error) {
		return []byte(secret), nil
	})
	if err != nil || !token.Valid {
		return nil, errors.New("invalid token")
	}
	if claims, ok := token.Claims.(jwt.MapClaims); ok {
		return claims, nil
	}
	return nil, errors.New("invalid claims")
}

// ExtractToken 从 Authorization header 提取 Token
func ExtractToken(authHeader string) string {
	if strings.HasPrefix(authHeader, "Bearer ") {
		return strings.TrimPrefix(authHeader, "Bearer ")
	}
	return ""
}