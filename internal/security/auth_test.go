// 单元测试：安全鉴权模块
package security

import (
	"testing"
	"github.com/stretchr/testify/assert"
)

func TestAPIKeyAuth(t *testing.T) {
	valid := []string{"key1", "key2"}
	assert.True(t, APIKeyAuth("key1", valid))
	assert.False(t, APIKeyAuth("invalid", valid))
	assert.False(t, APIKeyAuth("", valid))
}

func TestJWTAuth_Invalid(t *testing.T) {
	claims, err := JWTAuth("invalidtoken", "secret")
	assert.Nil(t, claims)
	assert.Error(t, err)
}

func TestExtractToken(t *testing.T) {
	token := ExtractToken("Bearer abcdef")
	assert.Equal(t, "abcdef", token)
	token = ExtractToken("abc")
	assert.Equal(t, "", token)
}