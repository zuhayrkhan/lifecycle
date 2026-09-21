package io.github.zuhayrkhan.lifecycle;

final class Names {
    private Names() {}

    static String requireName(String name) {
        if (name == null || name.isEmpty() || name.codePoints().allMatch(Names::isWhitespace)) {
            throw new IllegalArgumentException("Names and keys must be nonblank");
        }
        return name;
    }

    private static boolean isWhitespace(int c) {
        return (c >= 0x09 && c <= 0x0d) || c == 0x20 || c == 0x85 || c == 0xa0 || c == 0x1680
                || (c >= 0x2000 && c <= 0x200a) || c == 0x2028 || c == 0x2029
                || c == 0x202f || c == 0x205f || c == 0x3000;
    }
}
