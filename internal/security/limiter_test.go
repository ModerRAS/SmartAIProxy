// 单元测试：安全限流模块
package security

import (
	"testing"
	"github.com/stretchr/testify/assert"
	"time"
)

func TestLimiter_Allow_Normal(t *testing.T) {
	l := NewLimiter(2)
	assert.True(t, l.Allow("id1"))
	assert.True(t, l.Allow("id1"))
	assert.False(t, l.Allow("id1")) // 超过速率
	time.Sleep(time.Second)
	assert.True(t, l.Allow("id1")) // 令牌补充
}

func TestLimiter_Blacklist(t *testing.T) {
	l := NewLimiter(1)
	l.AddToBlacklist("id2")
	assert.False(t, l.Allow("id2"))
	l.RemoveFromBlacklist("id2")
	assert.True(t, l.Allow("id2"))
}

func TestLimiter_Whitelist(t *testing.T) {
	l := NewLimiter(1)
	l.AddToWhitelist("id3")
	assert.True(t, l.Allow("id3"))
	l.RemoveFromWhitelist("id3")
	assert.False(t, l.Allow("id3"))
}